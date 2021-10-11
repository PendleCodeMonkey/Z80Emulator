using PendleCodeMonkey.Z80EmulatorLib;
using PendleCodeMonkey.Z80EmulatorLib.Enumerations;
using Xunit;

namespace PendleCodeMonkey.Z80Emulator.Tests
{
	public class CPUTests
	{

		[Fact]
		public void NewCPU_ShouldNotBeNull()
		{
			CPU cpu = new CPU();

			Assert.NotNull(cpu);
		}

		[Fact]
		public void Reset_ShouldResetState()
		{
			CPU cpu = new CPU
			{
				A = 0x12,
				F = ProcessorFlags.Carry | ProcessorFlags.Zero,
				BC = 0x2345,
				DE = 0x3456,
				HL = 0x4567,
				IX = 0x5678,
				IY = 0x6789,
				PC = 0x0200,
				SP = 0x4000,
				I = 0x02,
				R = 0x03,
				IFF1 = true,
				IFF2 = true,
				InterruptMode = InterruptMode.Mode2,
				AF_Shadow = 0x6543,
				BC_Shadow = 0x5432,
				DE_Shadow = 0x4321,
				HL_Shadow = 0x3210
			};

			cpu.Reset();

			Assert.Equal(0x00, cpu.A);
			Assert.Equal((ProcessorFlags)0, cpu.F);
			Assert.Equal(0x0000, cpu.BC);
			Assert.Equal(0x0000, cpu.DE);
			Assert.Equal(0x0000, cpu.HL);
			Assert.Equal(0x0000, cpu.IX);
			Assert.Equal(0x0000, cpu.IY);
			Assert.Equal(0x0000, cpu.PC);
			Assert.Equal(0x0000, cpu.SP);
			Assert.Equal(0x00, cpu.I);
			Assert.Equal(0x00, cpu.R);
			Assert.False(cpu.IFF1);
			Assert.False(cpu.IFF2);
			Assert.Equal(InterruptMode.Mode0, cpu.InterruptMode);
			Assert.Equal(0x0000, cpu.AF_Shadow);
			Assert.Equal(0x0000, cpu.BC_Shadow);
			Assert.Equal(0x0000, cpu.DE_Shadow);
			Assert.Equal(0x0000, cpu.HL_Shadow);
		}

		[Fact]
		public void IncrementPC_ShouldIncrementProgramCounter()
		{
			CPU cpu = new CPU
			{
				PC = 0x0200
			};

			cpu.IncrementPC();

			Assert.Equal(0x0201, cpu.PC);
		}

		[Fact]
		public void AddPositiveOffsetToPC_ShouldIncreaseProgramCounter()
		{
			CPU cpu = new CPU
			{
				PC = 0x0200
			};

			cpu.AddOffsetToPC(0x60);

			Assert.Equal(0x0260, cpu.PC);
		}

		[Fact]
		public void AddNegativeOffsetToPC_ShouldDecreaseProgramCounter()
		{
			CPU cpu = new CPU
			{
				PC = 0x0200
			};

			cpu.AddOffsetToPC(-0x40);

			Assert.Equal(0x01C0, cpu.PC);
		}


		[Fact]
		public void ExchangeRegPairsWithShadow()
		{
			CPU cpu = new CPU
			{
				A = 0x12,
				F = ProcessorFlags.Carry | ProcessorFlags.Zero,
				BC = 0x2345,
				DE = 0x3456,
				HL = 0x4567,
				AF_Shadow = 0x6543,
				BC_Shadow = 0x5432,
				DE_Shadow = 0x4321,
				HL_Shadow = 0x3210
			};

			cpu.ExchangeRegPairsWithShadow();

			Assert.Equal(0x5432, cpu.BC);
			Assert.Equal(0x4321, cpu.DE);
			Assert.Equal(0x3210, cpu.HL);
			Assert.Equal(0x2345, cpu.BC_Shadow);
			Assert.Equal(0x3456, cpu.DE_Shadow);
			Assert.Equal(0x4567, cpu.HL_Shadow);
			// Check that A and F registers are unaffected.
			Assert.Equal(0x12, cpu.A);
			Assert.Equal(ProcessorFlags.Carry | ProcessorFlags.Zero, cpu.F);
			Assert.Equal(0x6543, cpu.AF_Shadow);
		}

		[Fact]
		public void ExchangeAFWithShadowAF()
		{
			CPU cpu = new CPU
			{
				A = 0x12,
				F = ProcessorFlags.Carry | ProcessorFlags.Zero,
				BC = 0x2345,
				DE = 0x3456,
				HL = 0x4567,
				AF_Shadow = 0x6500,
				BC_Shadow = 0x5432,
				DE_Shadow = 0x4321,
				HL_Shadow = 0x3210
			};

			cpu.ExchangeAFWithShadowAF();

			Assert.Equal(0x65, cpu.A);
			Assert.Equal((ProcessorFlags)0, cpu.F);
			Assert.Equal(0x1241, cpu.AF_Shadow);            // Carry and Zero flags give F value of 0x41 and A before exchange was 0x12
															// Check that BC, DE, HL registers are unaffected.
			Assert.Equal(0x2345, cpu.BC);
			Assert.Equal(0x3456, cpu.DE);
			Assert.Equal(0x4567, cpu.HL);
			Assert.Equal(0x5432, cpu.BC_Shadow);
			Assert.Equal(0x4321, cpu.DE_Shadow);
			Assert.Equal(0x3210, cpu.HL_Shadow);
		}

		[Fact]
		public void ReadRegster()
		{
			CPU cpu = new CPU
			{
				A = 0x12,
				BC = 0x2345,
				DE = 0x3456,
				HL = 0x4567,
			};

			var b = cpu.ReadRegister(0);
			var c = cpu.ReadRegister(1);
			var d = cpu.ReadRegister(2);
			var e = cpu.ReadRegister(3);
			var h = cpu.ReadRegister(4);
			var l = cpu.ReadRegister(5);
			var a = cpu.ReadRegister(7);

			Assert.Equal(0x12, a);
			Assert.Equal(0x23, b);
			Assert.Equal(0x45, c);
			Assert.Equal(0x34, d);
			Assert.Equal(0x56, e);
			Assert.Equal(0x45, h);
			Assert.Equal(0x67, l);
		}

		[Fact]
		public void WriteRegster()
		{
			CPU cpu = new CPU();

			cpu.WriteRegister(0, 0x12);
			cpu.WriteRegister(1, 0x23);
			cpu.WriteRegister(2, 0x34);
			cpu.WriteRegister(3, 0x45);
			cpu.WriteRegister(4, 0x56);
			cpu.WriteRegister(5, 0x67);
			cpu.WriteRegister(7, 0x78);

			Assert.Equal(0x12, cpu.B);
			Assert.Equal(0x23, cpu.C);
			Assert.Equal(0x34, cpu.D);
			Assert.Equal(0x45, cpu.E);
			Assert.Equal(0x56, cpu.H);
			Assert.Equal(0x67, cpu.L);
			Assert.Equal(0x78, cpu.A);
		}

		[Fact]
		public void ReadRegsterPair()
		{
			CPU cpu = new CPU
			{
				A = 0x12,
				F = ProcessorFlags.Carry | ProcessorFlags.Zero,
				BC = 0x2345,
				DE = 0x3456,
				HL = 0x4567,
				SP = 0x6000
			};

			var bc = cpu.ReadRegisterPair(0);
			var de = cpu.ReadRegisterPair(1);
			var hl = cpu.ReadRegisterPair(2);
			var sp = cpu.ReadRegisterPair(3);
			var af = cpu.ReadRegisterPair(3, true);

			Assert.Equal(0x2345, bc);
			Assert.Equal(0x3456, de);
			Assert.Equal(0x4567, hl);
			Assert.Equal(0x6000, sp);
			Assert.Equal(0x1241, af);
		}

		[Fact]
		public void WriteRegsterPair()
		{
			CPU cpu = new CPU();

			cpu.WriteRegisterPair(0, 0x1234);
			cpu.WriteRegisterPair(1, 0x2345);
			cpu.WriteRegisterPair(2, 0x3456);
			cpu.WriteRegisterPair(3, 0x4000);
			cpu.WriteRegisterPair(3, 0x4321, true);

			Assert.Equal(0x1234, cpu.BC);
			Assert.Equal(0x2345, cpu.DE);
			Assert.Equal(0x3456, cpu.HL);
			Assert.Equal(0x4000, cpu.SP);
			Assert.Equal(0x4321, cpu.AF);
		}

		[Theory]
		[InlineData((ProcessorFlags)0, 0, true)]
		[InlineData((ProcessorFlags)0, 1, false)]
		[InlineData(ProcessorFlags.Zero, 0, false)]
		[InlineData(ProcessorFlags.Zero, 1, true)]
		[InlineData((ProcessorFlags)0, 2, true)]
		[InlineData((ProcessorFlags)0, 3, false)]
		[InlineData(ProcessorFlags.Carry, 2, false)]
		[InlineData(ProcessorFlags.Carry, 3, true)]
		[InlineData((ProcessorFlags)0, 4, true)]
		[InlineData((ProcessorFlags)0, 5, false)]
		[InlineData(ProcessorFlags.ParityOverflow, 4, false)]
		[InlineData(ProcessorFlags.ParityOverflow, 5, true)]
		[InlineData((ProcessorFlags)0, 6, true)]
		[InlineData((ProcessorFlags)0, 7, false)]
		[InlineData(ProcessorFlags.Sign, 6, false)]
		[InlineData(ProcessorFlags.Sign, 7, true)]
		public void EvaluateCondition(ProcessorFlags flags, byte conditionIndex, bool expectedResult)
		{
			// Arrange
			CPU cpu = new CPU
			{
				F = flags
			};

			// Act
			bool result = cpu.EvaluateCondition(conditionIndex);

			// Assert
			Assert.Equal(expectedResult, result);
		}


		[Theory]
		[InlineData(0, 0x0000)]
		[InlineData(1, 0x0008)]
		[InlineData(2, 0x0010)]
		[InlineData(3, 0x0018)]
		[InlineData(4, 0x0020)]
		[InlineData(5, 0x0028)]
		[InlineData(6, 0x0030)]
		[InlineData(7, 0x0038)]
		[InlineData(40, 0x0000)]
		public void GetZeroPageAddress(byte zeroPageIndex, ushort expectedAddress)
		{
			// Arrange
			CPU cpu = new CPU();

			// Act
			ushort address = cpu.GetPageZeroAddress(zeroPageIndex);

			// Assert
			Assert.Equal(expectedAddress, address);
		}

	}
}
