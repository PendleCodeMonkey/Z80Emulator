using PendleCodeMonkey.Z80EmulatorLib;
using System;
using Xunit;

namespace PendleCodeMonkey.Z80Emulator.Tests
{
	public class InstructionDecoderTests
	{

		[Fact]
		public void NewDecoder_ShouldNotBeNull()
		{
			InstructionDecoder decoder = new InstructionDecoder(new Machine());

			Assert.NotNull(decoder);
		}

		[Fact]
		public void ReadNextPCByte_ShouldReturnValueWhenPCIsWithinLoadedData()
		{
			Machine machine = new Machine();
			InstructionDecoder decoder = new InstructionDecoder(machine);
			var _ = machine.LoadExecutableData(new byte[] { 1, 2, 3, 4, 5, 6 }, 0x2000);

			var value = decoder.ReadNextPCByte();

			Assert.Equal(1, value);
		}

		[Fact]
		public void ReadNextPCByte_ShouldThrowExceptionWhenPCIsPassedEndOfLoadedData()
		{
			Machine machine = new Machine();
			InstructionDecoder decoder = new InstructionDecoder(machine);
			var _ = machine.LoadExecutableData(new byte[] { 1, 2, 3, 4, 5, 6 }, 0x2000);
			machine.CPU.PC = 0x2008;

			Assert.Throws<InvalidOperationException>(() => decoder.ReadNextPCByte());
		}


		[Fact]
		public void FetchInstruction_ShouldFetchValidInstruction()
		{
			Machine machine = new Machine();
			InstructionDecoder decoder = new InstructionDecoder(machine);
			var _ = machine.LoadExecutableData(new byte[] { 0x01, 0x34, 0x30 }, 0x2000);

			var instruction = decoder.FetchInstruction();

			Assert.NotNull(instruction);
			Assert.Equal(0x01, instruction.Opcode);
			Assert.Equal(0x3034, instruction.WordOperand);
		}

	}
}
