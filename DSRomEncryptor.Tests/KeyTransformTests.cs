using NUnit.Framework;
using System.Security.Cryptography;

namespace DSRomEncryptor.Tests;

public class KeyTransformTests
{
    [Test]
    public void KeyTransform_TransformTableLevel2_ReturnsCorrectTransformedTable()
    {
        // Arrange
        Assume.That(new BlowfishLocator().TryGetNtrBlowfish(out var ntrBlowfish), Is.True,
            "Could not find ntr blowfish table.");

        // Act
        var table = KeyTransform.TransformTable(0x45434D41, 2, 8, ntrBlowfish);

        // Assert
        Assert.That(SHA1.HashData(table), Is.EqualTo(new byte[]
        {
            0xD7, 0x21, 0x5D, 0xED, 0xE5, 0xA9, 0xBF, 0x97, 0xFC, 0x75,
            0x33, 0x8B, 0x03, 0x7A, 0x68, 0x1F, 0x15, 0x8C, 0x3B, 0xBC
        }));
    }
}
