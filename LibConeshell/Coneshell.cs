using System.Security.Cryptography;
using LZ4;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Agreement;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace LibConeshell;

public static class Coneshell
{
    private const uint HeaderMagic = 0x0200DEC0;

    public static AsymmetricCipherKeyPair GenerateKeyPair()
    {
        var keygen = new X25519KeyPairGenerator();
        keygen.Init(new X25519KeyGenerationParameters(new SecureRandom()));

        var keypair = keygen.GenerateKeyPair();
        if (keypair == null)
            throw new InvalidDataException("Failed to generate x25519 keypair.");

        return keypair;
    }

    private static byte[] DeriveDeviceSecret(byte[] sharedSecret, byte[] deviceUdid)
    {
        if (sharedSecret.Length != 32)
            throw new InvalidDataException("The shared secret must be 32 bytes in length.");

        if (deviceUdid.Length != 16)
            throw new InvalidDataException("The device uuid must be 16 bytes in length.");

        var result = sharedSecret[..16];

        for (int i = 0; i < 16; i++)
        {
            var udid = deviceUdid[i];
            var secret = result[i];
            var mixed1 = udid ^ secret;
            var mixed2 = deviceUdid[mixed1 & 0xf] ^ udid;
            var mixed3 = result[mixed2 & 0xf] ^ udid;
            result[mixed3 & 0xf] ^= (byte) mixed2;
            result[mixed2 & 0xf] ^= (byte) mixed1;
            result[mixed1 & 0xf] ^= (byte) mixed3;
        }

        var hash = MD5.Create();
        hash.TransformBlock(sharedSecret, 0, sharedSecret.Length, null, 0);
        hash.TransformFinalBlock(result, 0, result.Length);

        return hash.Hash!;
    }

    private static byte[] AesCtrCryptInternal(byte[] message, byte[] key, byte[] iv)
    {
        if (key.Length != 16 || iv.Length != 16)
            throw new InvalidDataException("Both the key and iv must be 16 bytes in length.");

        var aes = Aes.Create();
        aes.KeySize = 128;
        aes.Padding = PaddingMode.None;
        aes.Mode = CipherMode.ECB;

        var counter = (byte[]) iv.Clone();

        var xorMask = new Queue<byte>();
        var transform = aes.CreateEncryptor(key, new byte[16]);

        using var inputStream = new MemoryStream(message);
        using var outputStream = new MemoryStream();

        int byteRead;
        while ((byteRead = inputStream.ReadByte()) != -1)
        {
            if (xorMask.Count == 0)
            {
                var ctrBlock = new byte[16];

                transform.TransformBlock(counter, 0, counter.Length, ctrBlock, 0);

                for (var j = counter.Length - 1; j >= 0; j--)
                {
                    if (++counter[j] != 0)
                        break;
                }

                foreach (var ctrByte in ctrBlock)
                    xorMask.Enqueue(ctrByte);
            }

            var ctrMask = xorMask.Dequeue();
            outputStream.WriteByte((byte)((byte)byteRead ^ ctrMask));
        }

        return outputStream.ToArray();
    }

    public static byte[] GenerateSharedSecret(X25519PublicKeyParameters pubKey, X25519PrivateKeyParameters privKey)
    {
        var agreement = new X25519Agreement();
        agreement.Init(privKey);

        var secret = new byte[agreement.AgreementSize];
        agreement.CalculateAgreement(pubKey, secret, 0);

        return secret;
    }

    public static (byte[] encrypted, byte[] secret) EncryptRequestMessage(byte[] message, X25519PublicKeyParameters serverPublicKey, byte[] clientUdid, X25519PrivateKeyParameters? clientPrivateKey = null, bool shouldCompress = false)
    {
        const int headerSize = 0x4 + 0x20 + 0x10;

        var encryptedBufferLength = headerSize + 0x4 + (shouldCompress ? LZ4Codec.MaximumOutputLength(message.Length) : message.Length);
        var encryptedBuffer = new byte[encryptedBufferLength];

        using var encryptedStream = new MemoryStream(encryptedBuffer);
        using var encryptedWriter = new BinaryWriter(encryptedStream);

        encryptedWriter.Write(HeaderMagic);

        X25519PublicKeyParameters clientPublicKey;

        if (clientPrivateKey == null)
        {
            var keypair = GenerateKeyPair(); 
            clientPrivateKey = (X25519PrivateKeyParameters)keypair.Private;
            clientPublicKey = (X25519PublicKeyParameters)keypair.Public;
        }
        else
        {
            clientPublicKey = clientPrivateKey.GeneratePublicKey();
        }

        var clientEncPubKey = clientPublicKey.GetEncoded();

        var sharedSecret = GenerateSharedSecret(serverPublicKey, clientPrivateKey);

        encryptedWriter.Write(clientEncPubKey);

        var key = DeriveDeviceSecret(sharedSecret, clientUdid);
        var ivHash = MD5.Create();

        ivHash.TransformBlock(clientEncPubKey, 0, clientEncPubKey.Length, null, 0);
        ivHash.TransformFinalBlock(clientUdid, 0, clientUdid.Length);
        var iv = ivHash.Hash!;

        var encrypted = EncryptMessageInternal(encryptedWriter, message, key, iv, clientUdid, clientEncPubKey, shouldCompress);

        return (encrypted, sharedSecret);
    }

    public static byte[] EncryptResponseMessage(byte[] message, byte[] sharedSecret, byte[] clientUdid, bool shouldCompress = false)
    {
        const int headerSize = 0x4 + 0x10 + 0x10;

        var encryptedBufferLength = headerSize + 0x4 + (shouldCompress ? LZ4Codec.MaximumOutputLength(message.Length) : message.Length);
        var encryptedBuffer = new byte[encryptedBufferLength];

        using var encryptedStream = new MemoryStream(encryptedBuffer);
        using var encryptedWriter = new BinaryWriter(encryptedStream);

        encryptedWriter.Write(HeaderMagic);

        var iv = RandomNumberGenerator.GetBytes(16);
        var key = DeriveDeviceSecret(sharedSecret, clientUdid);

        encryptedWriter.Write(iv);

        var encrypted = EncryptMessageInternal(encryptedWriter, message, key, iv, clientUdid, iv, shouldCompress);

        return encrypted;
    }

    private static byte[] EncryptMessageInternal(BinaryWriter encryptedWriter, byte[] message, byte[] key, byte[] iv,
        byte[] clientUdid, byte[] checksumBlock, bool shouldCompress = false)
    {
        var currentSize = encryptedWriter.BaseStream.Position;
        byte[] body;

        if (shouldCompress)
        {
            var compressed = LZ4Codec.Encode(message, 0, message.Length);
            body = new byte[compressed.Length + 4];
            body[0] = (byte)message.Length;
            body[1] = (byte) (message.Length >> 8);
            body[2] = (byte) (message.Length >> 16);
            body[3] = (byte) (message.Length >> 24);
            Buffer.BlockCopy(compressed, 0, body, 4, compressed.Length);
        }
        else
        {
            body = new byte[message.Length + 4];
            Buffer.BlockCopy(message, 0, body, 4, message.Length);
        }

        var encryptedBody = AesCtrCryptInternal(body, key, iv);

        using var firstHash = MD5.Create();
        firstHash.TransformBlock(clientUdid, 0, clientUdid.Length, null, 0);
        firstHash.TransformFinalBlock(body, 0, body.Length);
        var first = firstHash.Hash!;

        using var checksumHash = MD5.Create();
        checksumHash.TransformBlock(checksumBlock, 0, checksumBlock.Length, null, 0);
        checksumHash.TransformFinalBlock(first, 0, first.Length);
        var checksum = checksumHash.Hash!;

        encryptedWriter.Write(checksum);
        encryptedWriter.Write(encryptedBody);

        var encrypted = ((MemoryStream) encryptedWriter.BaseStream).ToArray();
        var expectedLength = currentSize + 0x10 + encryptedBody.Length;
        if (encrypted.Length != expectedLength)
        {
            var trimmed = new byte[expectedLength];
            Buffer.BlockCopy(encrypted, 0, trimmed, 0, (int) expectedLength);
            return trimmed;
        }

        return encrypted;
    }

    public static (byte[] message, byte[] secret) DecryptRequestMessage(byte[] encrypted, X25519PrivateKeyParameters serverPrivateKey, byte[] clientUdid)
    {
        const int headerSize = 0x4 + 0x20 + 0x10;

        using var inputStream = new MemoryStream(encrypted);
        using var inputReader = new BinaryReader(inputStream);

        if (inputReader.ReadUInt32() != HeaderMagic)
            throw new InvalidDataException("Invalid message header.");

        var clientEncPubKey = inputReader.ReadBytes(0x20);
        var expectedChecksum = inputReader.ReadBytes(0x10);

        var clientPubKey = new X25519PublicKeyParameters(clientEncPubKey);
        var sharedSecret = GenerateSharedSecret(clientPubKey, serverPrivateKey);

        var key = DeriveDeviceSecret(sharedSecret, clientUdid);

        using var ivHash = MD5.Create();
        ivHash.TransformBlock(clientEncPubKey, 0, clientEncPubKey.Length, null, 0);
        ivHash.TransformFinalBlock(clientUdid, 0, clientUdid.Length);
        var iv = ivHash.Hash!;

        var message = DecryptMessageInternal(inputReader.ReadBytes(encrypted.Length - headerSize), key, iv, clientUdid,
            clientEncPubKey, expectedChecksum);

        return (message, sharedSecret);
    }

    public static byte[] DecryptResponseMessage(byte[] encrypted, byte[] sharedSecret, byte[] clientUdid)
    {
        const int headerSize = 0x4 + 0x10 + 0x10;

        using var inputStream = new MemoryStream(encrypted);
        using var inputReader = new BinaryReader(inputStream);

        if (inputReader.ReadUInt32() != HeaderMagic)
            throw new InvalidDataException("Invalid message header.");

        var iv = inputReader.ReadBytes(16);
        var expectedChecksum = inputReader.ReadBytes(16);

        var key = DeriveDeviceSecret(sharedSecret, clientUdid);

        var message = DecryptMessageInternal(inputReader.ReadBytes(encrypted.Length - headerSize), key, iv, clientUdid,
            iv, expectedChecksum);

        return message;
    }

    private static byte[] DecryptMessageInternal(byte[] encryptedBody, byte[] key, byte[] iv, byte[] clientUdid,
        byte[] checksumBlock, byte[] expectedChecksum)
    {
        var body = AesCtrCryptInternal(encryptedBody, key, iv);

        using var firstHash = MD5.Create();
        firstHash.TransformBlock(clientUdid, 0, clientUdid.Length, null, 0);
        firstHash.TransformFinalBlock(body, 0, body.Length);
        var first = firstHash.Hash!;

        using var checksumHash = MD5.Create();
        checksumHash.TransformBlock(checksumBlock, 0, checksumBlock.Length, null, 0);
        checksumHash.TransformFinalBlock(first, 0, first.Length);
        var checksum = checksumHash.Hash!;

        if (!checksum.SequenceEqual(expectedChecksum))
            throw new InvalidDataException("Body checksum mismatch.");

        var decompressedLength = body[0] | (body[1] << 8) | (body[2] << 16) | (body[3] << 24);
        var bodyData = body.Skip(4).ToArray();

        return decompressedLength != 0 
            ? LZ4Codec.Decode(bodyData, 0, bodyData.Length, decompressedLength)
            : bodyData;
    }
}