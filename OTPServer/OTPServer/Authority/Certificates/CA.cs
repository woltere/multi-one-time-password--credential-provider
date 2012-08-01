using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Asn1;
using System.Collections;

namespace OTPServer.Authority.Certificates
{
    class CA
    {
        public static byte[] GenerateSignedCertificate(
            System.Security.Cryptography.X509Certificates.X509Certificate2 authorityDotNet, 
            string subjectName, 
            DateTime notBefore, 
            DateTime notAfter)
        {
            // Get authority cert and private key
            var authority = DotNetUtilities.FromX509Certificate(authorityDotNet);
            var authorityKp = DotNetUtilities.GetKeyPair(authorityDotNet.PrivateKey);

            // TODO: Caller must Reset()
            authorityDotNet.Reset();

            // Generate new key pair for subject's cert
            var kpgen = new RsaKeyPairGenerator();
            kpgen.Init(new KeyGenerationParameters(new SecureRandom(new CryptoApiRandomGenerator()), 2048));

            var kp = kpgen.GenerateKeyPair();

            // Set basic attributes
            var certName = new X509Name("CN=" + subjectName);
            var serialNo = BigInteger.ProbablePrime(120, new Random());

            var gen = new X509V3CertificateGenerator();

            gen.SetSerialNumber(serialNo);
            gen.SetSubjectDN(certName);
            gen.SetIssuerDN(authority.IssuerDN);
            gen.SetNotAfter(notAfter);
            gen.SetNotBefore(notBefore);
            gen.SetSignatureAlgorithm("MD5WithRSA");
            gen.SetPublicKey(kp.Public);

            // Add extensions
            gen.AddExtension(
                X509Extensions.AuthorityKeyIdentifier.Id,
                false,
                new AuthorityKeyIdentifier(
                    SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(authorityKp.Public),
                    new GeneralNames(new GeneralName(authority.SubjectDN)),
                    authority.SerialNumber));

            gen.AddExtension(
                X509Extensions.ExtendedKeyUsage.Id,
                false,
                new ExtendedKeyUsage(
                    Asn1Sequence.GetInstance(
                        new DerObjectIdentifier("1.3.6.1.5.5.7.3.1").GetDerEncoded()))); // client authentication

            // Generate and sign certificate
            var newCert = gen.Generate(authorityKp.Private);

            return DotNetUtilities.ToX509Certificate(newCert).Export(System.Security.Cryptography.X509Certificates.X509ContentType.SerializedCert);
        }

        public static System.Security.Cryptography.X509Certificates.X509Certificate2 GetCertificateFromMachineStore(
            string thumbPrint)
        {
            System.Security.Cryptography.X509Certificates.X509Certificate2 certificate = null;
            if (thumbPrint != null && thumbPrint != String.Empty)
            {                
                try
                {
                    System.Security.Cryptography.X509Certificates.X509Store 
                        store = new System.Security.Cryptography.X509Certificates.X509Store(
                            System.Security.Cryptography.X509Certificates.StoreName.My, 
                            System.Security.Cryptography.X509Certificates.StoreLocation.LocalMachine);

                    store.Open(
                        System.Security.Cryptography.X509Certificates.OpenFlags.ReadOnly 
                        | System.Security.Cryptography.X509Certificates.OpenFlags.OpenExistingOnly);

                    System.Security.Cryptography.X509Certificates.X509Certificate2Collection 
                        collection = store.Certificates.Find(
                            System.Security.Cryptography.X509Certificates.X509FindType.FindByTimeValid, 
                            DateTime.Now, 
                            false);

                    collection = collection.Find(
                        System.Security.Cryptography.X509Certificates.X509FindType.FindByThumbprint, 
                        thumbPrint, 
                        false);

                    if (collection.Count == 1)
                    {
                        certificate = collection[0];
                    }

                    collection.Clear();
                    store.Close();
                }
                catch (Exception)
                { }                
            }
            return certificate;
        }
    }
}
