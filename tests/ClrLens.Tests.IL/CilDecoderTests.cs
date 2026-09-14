using System.Reflection;
using ClrLens.IL;

namespace ClrLens.Tests.IL;

public sealed class CilDecoderTests
{
    [Fact]
    public void Decode_FixtureMethodPreservesBranchesAndMetadataTokens()
    {
        var decoded = TestSupport.DecodeFixtureMethod("ExceptionFlow");

        Assert.NotEmpty(decoded.Instructions);
        Assert.Contains(decoded.Instructions, i => i.BranchTargets.Count > 0);
        Assert.Contains(decoded.Instructions, i => i.Operand is int);
    }

    [Fact]
    public void DecodeInstructions_SwitchOperandPreservesAllTargets()
    {
        var decodeInstructions = typeof(CilDecoder)
            .GetMethod("DecodeInstructions", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new MissingMethodException("CilDecoder.DecodeInstructions");
        var diagnostics = new List<CilDiagnostic>();

        var il = new byte[]
        {
            0x45,
            0x02, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x04, 0x00, 0x00, 0x00,
            0x00,
            0x2A,
            0x00,
            0x2A
        };

        var instructions = (List<CilInstruction>)decodeInstructions.Invoke(null, [il, diagnostics])!;
        var first = Assert.Single(instructions, i => i.OpCode.Name == "switch");

        Assert.Equal(2, first.BranchTargets.Count);
        Assert.Equal(13, first.BranchTargets[0]);
        Assert.Equal(17, first.BranchTargets[1]);
        Assert.Empty(diagnostics);
    }
}
