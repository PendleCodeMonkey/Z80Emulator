using PendleCodeMonkey.Z80EmulatorLib;
using PendleCodeMonkey.Z80EmulatorLib.Enumerations;
using Xunit;

namespace PendleCodeMonkey.Z80Emulator.Tests
{
	public class MachineTests
	{
		[Fact]
		public void NewMachine_ShouldNotBeNull()
		{
			Machine machine = new Machine();

			Assert.NotNull(machine);
		}

		[Fact]
		public void NewMachine_ShouldHaveCPU()
		{
			Machine machine = new Machine();

			Assert.NotNull(machine.CPU);
		}

		[Fact]
		public void NewMachine_ShouldHaveMemory()
		{
			Machine machine = new Machine();

			Assert.NotNull(machine.Memory);
		}

		[Fact]
		public void NewMachine_ShouldHaveAStack()
		{
			Machine machine = new Machine();

			Assert.NotNull(machine.Stack);
		}

		[Fact]
		public void NewMachine_ShouldHaveExecutionHandler()
		{
			Machine machine = new Machine();

			Assert.NotNull(machine.ExecutionHandler);
		}

		[Fact]
		public void LoadData_ShouldSucceedWhenDataFitsInMemory()
		{
			Machine machine = new Machine();
			var success = machine.LoadData(new byte[] { 1, 2, 3, 4, 5, 6 }, 0x2000);

			Assert.True(success);
		}

		[Fact]
		public void LoadData_ShouldFailWhenDataExceedsMemoryLimit()
		{
			Machine machine = new Machine();
			var success = machine.LoadData(new byte[] { 1, 2, 3, 4, 5, 6 }, 0xFFFC);

			Assert.False(success);
		}

		[Fact]
		public void LoadExecutableData_ShouldSetPCToStartOfLoadedData()
		{
			Machine machine = new Machine();
			var _ = machine.LoadExecutableData(new byte[] { 1, 2, 3, 4, 5, 6 }, 0x2000);

			Assert.Equal(0x2000, machine.CPU.PC);
		}

		[Fact]
		public void IsEndOfData_ShouldBeFalseWhenPCIsWithinLoadedData()
		{
			Machine machine = new Machine();
			var _ = machine.LoadExecutableData(new byte[] { 1, 2, 3, 4, 5, 6 }, 0x2000);

			Assert.False(machine.IsEndOfData);
		}

		[Fact]
		public void IsEndOfData_ShouldBeTrueWhenPCIsPassedEndOfLoadedData()
		{
			Machine machine = new Machine();
			var _ = machine.LoadExecutableData(new byte[] { 1, 2, 3, 4, 5, 6 }, 0x2000);
			machine.CPU.PC = 0x2008;

			Assert.True(machine.IsEndOfData);
		}

		[Fact]
		public void DumpMemory_ShouldReturnCorrectMemoryBlock()
		{
			Machine machine = new Machine();
			var _ = machine.LoadData(new byte[] { 1, 2, 3, 4, 5, 6 }, 0x2000);

			var dump = machine.DumpMemory(0x2002, 0x0010);

			Assert.Equal(0x0010, dump.Length);
			Assert.Equal(0x05, dump[2]);
		}

		[Fact]
		public void GetCPUState_ShouldReturnCorrectCPUState()
		{
			Machine machine = new Machine();
			machine.CPU.A = 0x12;
			machine.CPU.F = ProcessorFlags.Carry | ProcessorFlags.Zero;
			machine.CPU.BC = 0x2345;
			machine.CPU.DE = 0x3456;
			machine.CPU.HL = 0x4567;
			machine.CPU.AF_Shadow = 0x6543;
			machine.CPU.BC_Shadow = 0x5432;
			machine.CPU.DE_Shadow = 0x4321;
			machine.CPU.HL_Shadow = 0x3210;
			machine.CPU.PC = 0x2233;
			machine.CPU.SP = 0x3344;
			machine.CPU.I = 0x12;
			machine.CPU.R = 0x23;
			machine.CPU.IFF1 = true;
			machine.CPU.IFF2 = true;
			machine.CPU.InterruptMode = InterruptMode.Mode1;

			CPUState state = machine.GetCPUState();

			Assert.Equal(machine.CPU.AF, state.AF);
			Assert.Equal(machine.CPU.BC, state.BC);
			Assert.Equal(machine.CPU.DE, state.DE);
			Assert.Equal(machine.CPU.HL, state.HL);
			Assert.Equal(machine.CPU.PC, state.PC);
			Assert.Equal(machine.CPU.SP, state.SP);
			Assert.Equal(machine.CPU.AF_Shadow, state.AF_Shadow);
			Assert.Equal(machine.CPU.BC_Shadow, state.BC_Shadow);
			Assert.Equal(machine.CPU.DE_Shadow, state.DE_Shadow);
			Assert.Equal(machine.CPU.HL_Shadow, state.HL_Shadow);
			Assert.Equal(machine.CPU.I, state.I);
			Assert.Equal(machine.CPU.R, state.R);
			Assert.Equal(machine.CPU.IFF1, state.IFF1);
			Assert.Equal(machine.CPU.IFF2, state.IFF2);
			Assert.Equal(machine.CPU.InterruptMode, state.InterruptMode);
		}

		[Fact]
		public void SetCPUState_ShouldCorrectlyUpdateCPUState()
		{
			Machine machine = new Machine();

			// Set initial state of CPU.
			machine.CPU.A = 0x12;
			machine.CPU.F = ProcessorFlags.Carry | ProcessorFlags.Zero;
			machine.CPU.BC = 0x2345;
			machine.CPU.DE = 0x3456;
			machine.CPU.HL = 0x4567;
			machine.CPU.AF_Shadow = 0x6543;
			machine.CPU.BC_Shadow = 0x5432;
			machine.CPU.DE_Shadow = 0x4321;
			machine.CPU.HL_Shadow = 0x3210;
			machine.CPU.PC = 0x2233;
			machine.CPU.SP = 0x3344;
			machine.CPU.I = 0x12;
			machine.CPU.R = 0x23;
			machine.CPU.IFF1 = true;
			machine.CPU.IFF2 = true;
			machine.CPU.InterruptMode = InterruptMode.Mode1;

			// Set the state of some settings (NOTE: all others should remain unchanged)
			CPUState newState = new CPUState
			{
				AF = 0x2030,
				DE = 0x1050,
				BC_Shadow = 0xABCD,
				PC = 0x8000,
				R = 0x40,
				IFF2 = false,
				InterruptMode = InterruptMode.Mode2
			};
			machine.SetCPUState(newState);

			Assert.Equal(0x2030, machine.CPU.AF);
			Assert.Equal(0x2345, machine.CPU.BC);
			Assert.Equal(0x1050, machine.CPU.DE);
			Assert.Equal(0x4567, machine.CPU.HL);
			Assert.Equal(0x8000, machine.CPU.PC);
			Assert.Equal(0x3344, machine.CPU.SP);
			Assert.Equal(0x6543, machine.CPU.AF_Shadow);
			Assert.Equal(0xABCD, machine.CPU.BC_Shadow);
			Assert.Equal(0x4321, machine.CPU.DE_Shadow);
			Assert.Equal(0x3210, machine.CPU.HL_Shadow);
			Assert.Equal(0x12, machine.CPU.I);
			Assert.Equal(0x40, machine.CPU.R);
			Assert.True(machine.CPU.IFF1);
			Assert.False(machine.CPU.IFF2);
			Assert.Equal(InterruptMode.Mode2, machine.CPU.InterruptMode);
		}

	}
}
