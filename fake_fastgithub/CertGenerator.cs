﻿using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.X509.Extension;
using System.Net;
using System.Text;
using X509Certificate2 = System.Security.Cryptography.X509Certificates.X509Certificate2;

namespace fake_fastgithub
{
    static class CertGenerator
    {
        private static readonly SecureRandom secureRandom = new();

        public static void GenerateBySelf(
            IEnumerable<string> domains,
            int keySizeBits,
            DateTime validFrom,
            DateTime validTo,
            string caPublicCerPath,
            string caPrivateKeyPath)
        {
            var keys = GenerateRsaKeyPair(keySizeBits);
            var cert = GenerateCertificate(domains, keys.Public, validFrom, validTo, domains.First(), null, keys.Private, 1);

            using var priWriter = new StreamWriter(caPrivateKeyPath);
            var priPemWriter = new PemWriter(priWriter);
            priPemWriter.WriteObject(keys.Private);
            priPemWriter.Writer.Flush();

            using var pubWriter = new StreamWriter(caPublicCerPath);
            var pubPemWriter = new PemWriter(pubWriter);
            pubPemWriter.WriteObject(cert);
            pubPemWriter.Writer.Flush();
        }

        public static X509Certificate2 GenerateByCa(
            IEnumerable<string> domains,
            int keySizeBits,
            DateTime validFrom,
            DateTime validTo,
            string caPublicCerPath,
            string caPrivateKeyPath,
            string? password = default)
        {
            if (File.Exists(caPublicCerPath) == false)
            {
                throw new FileNotFoundException(caPublicCerPath);
            }

            if (File.Exists(caPrivateKeyPath) == false)
            {
                throw new FileNotFoundException(caPublicCerPath);
            }

            using var pubReader = new StreamReader(caPublicCerPath, Encoding.ASCII);
            var caCert = (X509Certificate)new PemReader(pubReader).ReadObject();

            using var priReader = new StreamReader(caPrivateKeyPath, Encoding.ASCII);
            var reader = new PemReader(priReader);
            var caPrivateKey = ((AsymmetricCipherKeyPair)reader.ReadObject()).Private;

            var caSubjectName = GetSubjectName(caCert);
            var keys = GenerateRsaKeyPair(keySizeBits);
            var cert = GenerateCertificate(domains, keys.Public, validFrom, validTo, caSubjectName, caCert.GetPublicKey(), caPrivateKey, null);

            return GeneratePfx(cert, keys.Private, password);
        }

        private static AsymmetricCipherKeyPair GenerateRsaKeyPair(int length)
        {
            var keygenParam = new KeyGenerationParameters(secureRandom, length);
            var keyGenerator = new RsaKeyPairGenerator();
            keyGenerator.Init(keygenParam);
            return keyGenerator.GenerateKeyPair();
        }

        private static X509Certificate GenerateCertificate(
            IEnumerable<string> domains,
            AsymmetricKeyParameter subjectPublic,
            DateTime validFrom,
            DateTime validTo,
            string issuerName,
            AsymmetricKeyParameter? issuerPublic,
            AsymmetricKeyParameter issuerPrivate,
            int? caPathLengthConstraint)
        {
            var signatureFactory = issuerPrivate is ECPrivateKeyParameters
                ? new Asn1SignatureFactory(X9ObjectIdentifiers.ECDsaWithSha256.ToString(), issuerPrivate)
                : new Asn1SignatureFactory(PkcsObjectIdentifiers.Sha256WithRsaEncryption.ToString(), issuerPrivate);

            var certGenerator = new X509V3CertificateGenerator();
            certGenerator.SetIssuerDN(new X509Name("CN=" + issuerName));
            certGenerator.SetSubjectDN(new X509Name("CN=" + domains.First()));
            certGenerator.SetSerialNumber(BigInteger.ProbablePrime(120, new Random()));
            certGenerator.SetNotBefore(validFrom);
            certGenerator.SetNotAfter(validTo);
            certGenerator.SetPublicKey(subjectPublic);

            if (issuerPublic != null)
            {
                var akis = new AuthorityKeyIdentifierStructure(issuerPublic);
                certGenerator.AddExtension(X509Extensions.AuthorityKeyIdentifier, false, akis);
            }

            if (caPathLengthConstraint != null && caPathLengthConstraint >= 0)
            {
                var basicConstraints = new BasicConstraints(caPathLengthConstraint.Value);
                certGenerator.AddExtension(X509Extensions.BasicConstraints, true, basicConstraints);
                certGenerator.AddExtension(X509Extensions.KeyUsage, false, new KeyUsage(KeyUsage.DigitalSignature | KeyUsage.CrlSign | KeyUsage.KeyCertSign));
            }
            else
            {
                var basicConstraints = new BasicConstraints(cA: false);
                certGenerator.AddExtension(X509Extensions.BasicConstraints, true, basicConstraints);
                certGenerator.AddExtension(X509Extensions.KeyUsage, false, new KeyUsage(KeyUsage.DigitalSignature | KeyUsage.KeyEncipherment));
            }
            certGenerator.AddExtension(X509Extensions.ExtendedKeyUsage, true, new ExtendedKeyUsage(KeyPurposeID.IdKPServerAuth));

            var names = domains.Select(domain =>
            {
                var nameType = GeneralName.DnsName;
                if (IPAddress.TryParse(domain, out _))
                {
                    nameType = GeneralName.IPAddress;
                }
                return new GeneralName(nameType, domain);
            }).ToArray();

            var subjectAltName = new GeneralNames(names);
            certGenerator.AddExtension(X509Extensions.SubjectAlternativeName, false, subjectAltName);
            return certGenerator.Generate(signatureFactory);
        }

        private static X509Certificate2 GeneratePfx(X509Certificate cert, AsymmetricKeyParameter privateKey, string? password)
        {
            var subject = GetSubjectName(cert);
            var pkcs12Store = new Pkcs12Store();
            var certEntry = new X509CertificateEntry(cert);
            pkcs12Store.SetCertificateEntry(subject, certEntry);
            pkcs12Store.SetKeyEntry(subject, new AsymmetricKeyEntry(privateKey), new[] { certEntry });

            using var pfxStream = new MemoryStream();
            pkcs12Store.Save(pfxStream, password?.ToCharArray(), secureRandom);
            return new X509Certificate2(pfxStream.ToArray());
        }

        private static string GetSubjectName(X509Certificate cert)
        {
            var subject = cert.SubjectDN.ToString();
            if (subject.StartsWith("CN=", StringComparison.OrdinalIgnoreCase))
            {
                subject = subject[3..];
            }
            return subject;
        }
    }
}
