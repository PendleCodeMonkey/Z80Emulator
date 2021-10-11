using PendleCodeMonkey.Z80EmulatorLib;
using Xunit;

namespace PendleCodeMonkey.Z80Emulator.Tests
{
	public class StackTests
	{
		[Fact]
		public void NewStack_ShouldNotBeNull()
		{
			Memory memory = new Memory();
			CPU cpu = new CPU();
			Stack stack = new Stack(cpu, memory);

			Assert.NotNull(stack);
		}

		[Fact]
		public void Push_ShouldStoreValueAndDecrementStackPointer()
		{
			Memory mem = new Memory();
			CPU cpu = new CPU();
			Stack stack = new Stack(cpu, mem);
			cpu.SP = 0x2000;

			stack.Push(0x4050);

			Assert.Equal(0x1FFE, cpu.SP);
			Assert.Equal(0x40, mem.Data[0x01FFF]);
			Assert.Equal(0x50, mem.Data[0x01FFE]);
		}

		[Fact]
		public void Pop_ShouldIncrementStackPointerAndRetrieveValue()
		{
			Memory mem = new Memory();
			CPU cpu = new CPU();
			Stack stack = new Stack(cpu, mem);
			cpu.SP = 0x1FFE;
			mem.Data[0x01FFE] = 0x30;
			mem.Data[0x01FFF] = 0x40;

			ushort value = stack.Pop();

			Assert.Equal(0x2000, cpu.SP);
			Assert.Equal(0x4030, value);
		}

	}
}
