using PendleCodeMonkey.Z80EmulatorLib;
using PendleCodeMonkey.Z80EmulatorLib.Enumerations;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace PendleCodeMonkey.Z80Emulator.Tests
{
	public class OpcodeExecutionHandlerTests
	{
		//
		// Tests the methods that perform the execution of a single Z80 instruction.
		//
		// These all follow this pattern:
		// 1) Arrange:
		//		a) Create a Z80 Machine
		//		b) Load executable data into the machine (i.e. just the binary code data required for the instruction being tested)
		//		c) Load other data into memory as required (e.g. when testing instructions that use addressing modes that involve accessing memory)
		//		d) Initialize the state of the machine as required (e.g. initializing register values, flags, etc.)
		// 2) Act:
		//		Get the machine to execute the code.
		// 3) Assert:
		//		Assert the results of executing the code (i.e. checking register values, flags, memory contents, etc.)
		//


		// ***********************
		//
		// Unprefixed instructions
		//
		// ***********************

		[Fact]
		public void LDRegister_Register()
		{
			// Arrange
			byte[] code = new byte[] { 0x42, 0x55, 0x7C };  // LD B,D | LD D,L | LD A,H
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				DE = 0x6000,			// D = 0x60, E = 0x00
				HL = 0x1234
			};
			machine.SetCPUState(initState);

			// Act
			machine.Execute();

			// Assert
			Assert.Equal(0x60, machine.CPU.B);
			Assert.Equal(0x34, machine.CPU.D);
			Assert.Equal(0x12, machine.CPU.A);
		}

		[Fact]
		public void LDRegister_Immediate()
		{
			// Arrange
			byte[] code = new byte[] { 0x06, 0x88, 0x1E, 0xAA, 0x3E, 0x55 };  // LD B,#0x88 | LD E,#0xAA | LD A,#0x55
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);

			// Act
			machine.Execute();

			// Assert
			Assert.Equal(0x88, machine.CPU.B);
			Assert.Equal(0xAA, machine.CPU.E);
			Assert.Equal(0x55, machine.CPU.A);
		}

		[Fact]
		public void LDRegisterHL()
		{
			// Arrange
			byte[] code = new byte[] { 0x56, 0x7E };  // LD D,(HL) | LD A,(HL)
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				HL = 0x1000
			};
			machine.SetCPUState(initState);
			byte[] data = new byte[] { 0xAB };
			machine.LoadData(data, 0x1000, false);

			// Act
			machine.Execute();

			// Assert
			Assert.Equal(0xAB, machine.CPU.D);
			Assert.Equal(0xAB, machine.CPU.A);
		}

		[Fact]
		public void LDHLRegister()
		{
			// Arrange
			byte[] code = new byte[] { 0x71 };  // LD (HL),C
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				HL = 0x1000,
				BC = 0x0045,
			};
			machine.SetCPUState(initState);

			// Act
			machine.Execute();

			// Assert
			Assert.Equal(0x45, machine.Memory.Data[0x1000]);
		}

		[Theory]
		[InlineData(0x82, 0x72, (ProcessorFlags)0)]															// 0x82 is ADD A,D
		[InlineData(0x83, 0x82, ProcessorFlags.Sign | ProcessorFlags.ParityOverflow)]						// 0x83 is ADD A,E
		[InlineData(0x84, 0xBC, ProcessorFlags.Sign)]														// 0x84 is ADD A,H
		[InlineData(0x85, 0x00, ProcessorFlags.Zero | ProcessorFlags.Carry | ProcessorFlags.HalfCarry)]     // 0x85 is ADD A,L
		public void ADDAccRegister_Register(byte instruction, byte expectedResult, ProcessorFlags expectedFlags)
		{
			// Arrange
			byte[] code = new byte[] { instruction };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				DE = 0x6070,
				HL = 0xAAEE,
				AF = 0x1200
			};
			machine.SetCPUState(initState);

			// Act
			machine.Execute();

			// Assert
			Assert.Equal(expectedResult, machine.CPU.A);
			Assert.Equal(expectedFlags, machine.CPU.F);
		}

		[Theory]
		[InlineData(new byte[] { 0xC6, 0x40 }, 0x52, (ProcessorFlags)0)]
		[InlineData(new byte[] { 0xC6, 0x70 }, 0x82, ProcessorFlags.Sign | ProcessorFlags.ParityOverflow)]
		[InlineData(new byte[] { 0xC6, 0xAA }, 0xBC, ProcessorFlags.Sign)]
		[InlineData(new byte[] { 0xC6, 0xEE }, 0x00, ProcessorFlags.Zero | ProcessorFlags.Carry | ProcessorFlags.HalfCarry)]
		public void ADDAccRegister_Immediate(byte[] code, byte expectedResult, ProcessorFlags expectedFlags)
		{
			// Arrange
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				AF = 0x1200
			};
			machine.SetCPUState(initState);

			// Act
			machine.Execute();

			// Assert
			Assert.Equal(expectedResult, machine.CPU.A);
			Assert.Equal(expectedFlags, machine.CPU.F);
		}

		[Fact]
		public void ADDAccHL()
		{
			// Arrange
			byte[] code = new byte[] { 0x86 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				HL = 0x1000,
				AF = 0x1200
			};
			machine.SetCPUState(initState);
			machine.Memory.LoadData(new byte[] { 0xEE }, 0x1000, false);

			// Act
			machine.Execute();

			// Assert
			Assert.Equal(0x00, machine.CPU.A);
			Assert.Equal(ProcessorFlags.Zero | ProcessorFlags.Carry | ProcessorFlags.HalfCarry, machine.CPU.F);
		}

		[Theory]
		[InlineData(0x8A, 0x73, (ProcessorFlags)0)]                                                         // 0x8A is ADC A,D
		[InlineData(0x8B, 0x83, ProcessorFlags.Sign | ProcessorFlags.ParityOverflow)]                       // 0x8B is ADC A,E
		[InlineData(0x8C, 0xBD, ProcessorFlags.Sign)]                                                       // 0x8C is ADC A,H
		[InlineData(0x8D, 0x01, ProcessorFlags.Carry | ProcessorFlags.HalfCarry)]							// 0x8D is ADC A,L
		public void ADCAccRegister_Register(byte instruction, byte expectedResult, ProcessorFlags expectedFlags)
		{
			// Arrange
			byte[] code = new byte[] { instruction };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				DE = 0x6070,
				HL = 0xAAEE,
				AF = 0x1201				// F has Carry flag set
			};
			machine.SetCPUState(initState);

			// Act
			machine.Execute();

			// Assert
			Assert.Equal(expectedResult, machine.CPU.A);
			Assert.Equal(expectedFlags, machine.CPU.F);
		}

		[Theory]
		[InlineData(new byte[] { 0xCE, 0x40 }, 0x53, (ProcessorFlags)0)]
		[InlineData(new byte[] { 0xCE, 0x70 }, 0x83, ProcessorFlags.Sign | ProcessorFlags.ParityOverflow)]
		[InlineData(new byte[] { 0xCE, 0xAA }, 0xBD, ProcessorFlags.Sign)]
		[InlineData(new byte[] { 0xCE, 0xEE }, 0x01, ProcessorFlags.Carry | ProcessorFlags.HalfCarry)]
		public void ADCAccRegister_Immediate(byte[] code, byte expectedResult, ProcessorFlags expectedFlags)
		{
			// Arrange
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				AF = 0x1201             // F has Carry flag set
			};
			machine.SetCPUState(initState);

			// Act
			machine.Execute();

			// Assert
			Assert.Equal(expectedResult, machine.CPU.A);
			Assert.Equal(expectedFlags, machine.CPU.F);
		}

		[Fact]
		public void ADCAccHL()
		{
			// Arrange
			byte[] code = new byte[] { 0x8E };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				HL = 0x1000,
				AF = 0x1201             // F has Carry flag set
			};
			machine.SetCPUState(initState);
			machine.Memory.LoadData(new byte[] { 0xED }, 0x1000, false);

			// Act
			machine.Execute();

			// Assert
			Assert.Equal(0x00, machine.CPU.A);
			Assert.Equal(ProcessorFlags.Zero | ProcessorFlags.Carry | ProcessorFlags.HalfCarry, machine.CPU.F);
		}

		[Theory]
		[InlineData(0x92, 0x02, ProcessorFlags.Subtract)]                                                   // 0x92 is SUB A,D
		[InlineData(0x93, 0xF2, ProcessorFlags.Sign | ProcessorFlags.Carry | ProcessorFlags.Subtract)]		// 0x93 is SUB A,E
		[InlineData(0x94, 0xB8, ProcessorFlags.Sign | ProcessorFlags.Subtract | ProcessorFlags.Carry | ProcessorFlags.ParityOverflow | ProcessorFlags.HalfCarry)]    // 0x94 is SUB A,H
		[InlineData(0x95, 0x00, ProcessorFlags.Zero | ProcessorFlags.Subtract)]								// 0x95 is SUB A,L
		public void SUBAccRegister_Register(byte instruction, byte expectedResult, ProcessorFlags expectedFlags)
		{
			// Arrange
			byte[] code = new byte[] { instruction };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				DE = 0x6070,
				HL = 0xAA62,
				AF = 0x6200
			};
			machine.SetCPUState(initState);

			// Act
			machine.Execute();

			// Assert
			Assert.Equal(expectedResult, machine.CPU.A);
			Assert.Equal(expectedFlags, machine.CPU.F);
		}

		[Theory]
		[InlineData(new byte[] { 0xD6, 0x40 }, 0x22, ProcessorFlags.Subtract)]
		[InlineData(new byte[] { 0xD6, 0x70 }, 0xF2, ProcessorFlags.Sign | ProcessorFlags.Carry | ProcessorFlags.Subtract)]
		[InlineData(new byte[] { 0xD6, 0xAA }, 0xB8, ProcessorFlags.Sign | ProcessorFlags.Subtract | ProcessorFlags.Carry | ProcessorFlags.ParityOverflow | ProcessorFlags.HalfCarry)]
		[InlineData(new byte[] { 0xD6, 0x62 }, 0x00, ProcessorFlags.Zero | ProcessorFlags.Subtract)]
		public void SUBAccRegister_Immediate(byte[] code, byte expectedResult, ProcessorFlags expectedFlags)
		{
			// Arrange
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				AF = 0x6200
			};
			machine.SetCPUState(initState);

			// Act
			machine.Execute();

			// Assert
			Assert.Equal(expectedResult, machine.CPU.A);
			Assert.Equal(expectedFlags, machine.CPU.F);
		}

		[Fact]
		public void SUBAccHL()
		{
			// Arrange
			byte[] code = new byte[] { 0x96 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				HL = 0x1000,
				AF = 0x1200
			};
			machine.SetCPUState(initState);
			machine.Memory.LoadData(new byte[] { 0xAB }, 0x1000, false);

			// Act
			machine.Execute();

			// Assert
			Assert.Equal(0x67, machine.CPU.A);
			Assert.Equal(ProcessorFlags.Subtract | ProcessorFlags.Carry | ProcessorFlags.HalfCarry, machine.CPU.F);
		}


		[Theory]
		[InlineData(0x9A, 0x01, ProcessorFlags.Subtract)]                                                   // 0x9A is SBC A,D
		[InlineData(0x9B, 0xF1, ProcessorFlags.Sign | ProcessorFlags.Carry | ProcessorFlags.Subtract)]      // 0x9B is SBC A,E
		[InlineData(0x9C, 0xB7, ProcessorFlags.Sign | ProcessorFlags.Subtract | ProcessorFlags.Carry | ProcessorFlags.ParityOverflow | ProcessorFlags.HalfCarry)]    // 0x9C is SBC A,H
		[InlineData(0x9D, 0xFF, ProcessorFlags.Sign | ProcessorFlags.Subtract | ProcessorFlags.HalfCarry | ProcessorFlags.Carry)]      // 0x9D is SBC A,L
		public void SBCAccRegister_Register(byte instruction, byte expectedResult, ProcessorFlags expectedFlags)
		{
			// Arrange
			byte[] code = new byte[] { instruction };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				DE = 0x6070,
				HL = 0xAA62,
				AF = 0x6201
			};
			machine.SetCPUState(initState);

			// Act
			machine.Execute();

			// Assert
			Assert.Equal(expectedResult, machine.CPU.A);
			Assert.Equal(expectedFlags, machine.CPU.F);
		}

		[Theory]
		[InlineData(new byte[] { 0xDE, 0x40 }, 0x21, ProcessorFlags.Subtract)]
		[InlineData(new byte[] { 0xDE, 0x70 }, 0xF1, ProcessorFlags.Sign | ProcessorFlags.Carry | ProcessorFlags.Subtract)]
		[InlineData(new byte[] { 0xDE, 0xAA }, 0xB7, ProcessorFlags.Sign | ProcessorFlags.Subtract | ProcessorFlags.Carry | ProcessorFlags.ParityOverflow | ProcessorFlags.HalfCarry)]
		[InlineData(new byte[] { 0xDE, 0x61 }, 0x00, ProcessorFlags.Zero | ProcessorFlags.Subtract)]
		public void SBCAccRegister_Immediate(byte[] code, byte expectedResult, ProcessorFlags expectedFlags)
		{
			// Arrange
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				AF = 0x6201
			};
			machine.SetCPUState(initState);

			// Act
			machine.Execute();

			// Assert
			Assert.Equal(expectedResult, machine.CPU.A);
			Assert.Equal(expectedFlags, machine.CPU.F);
		}

		[Fact]
		public void SBCAccHL()
		{
			// Arrange
			byte[] code = new byte[] { 0x9E };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				HL = 0x1000,
				AF = 0x1201
			};
			machine.SetCPUState(initState);
			machine.Memory.LoadData(new byte[] { 0xAB }, 0x1000, false);

			// Act
			machine.Execute();

			// Assert
			Assert.Equal(0x66, machine.CPU.A);
			Assert.Equal(ProcessorFlags.Subtract | ProcessorFlags.Carry | ProcessorFlags.HalfCarry, machine.CPU.F);
		}

		[Theory]
		[InlineData(0xA0, 0x20, ProcessorFlags.HalfCarry)]                                          // 0xA0 is AND B
		[InlineData(0xA1, 0x80, ProcessorFlags.Sign | ProcessorFlags.HalfCarry)]					// 0xA1 is AND C
		[InlineData(0xA4, 0x00, ProcessorFlags.Zero | ProcessorFlags.ParityOverflow | ProcessorFlags.HalfCarry)]	 // 0xA4 is AND H
		[InlineData(0xA5, 0xA3, ProcessorFlags.Sign | ProcessorFlags.ParityOverflow | ProcessorFlags.HalfCarry)]     // 0xA5 is AND L
		public void ANDRegister_Register(byte instruction, byte expectedResult, ProcessorFlags expectedFlags)
		{
			// Arrange
			byte[] code = new byte[] { instruction };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				BC = 0x6080,
				HL = 0x00A3,
				AF = 0xA700
			};
			machine.SetCPUState(initState);

			// Act
			machine.Execute();

			// Assert
			Assert.Equal(expectedResult, machine.CPU.A);
			Assert.Equal(expectedFlags, machine.CPU.F);
		}

		[Theory]
		[InlineData(new byte[] { 0xE6, 0x40 }, 0x00, ProcessorFlags.Zero | ProcessorFlags.HalfCarry | ProcessorFlags.ParityOverflow)]
		[InlineData(new byte[] { 0xE6, 0x77 }, 0x22, ProcessorFlags.HalfCarry | ProcessorFlags.ParityOverflow)]
		[InlineData(new byte[] { 0xE6, 0x80 }, 0x80, ProcessorFlags.Sign | ProcessorFlags.HalfCarry)]
		[InlineData(new byte[] { 0xE6, 0x0F }, 0x0A, ProcessorFlags.HalfCarry | ProcessorFlags.ParityOverflow)]
		public void ANDRegister_Immediate(byte[] code, byte expectedResult, ProcessorFlags expectedFlags)
		{
			// Arrange
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				AF = 0xAA01
			};
			machine.SetCPUState(initState);

			// Act
			machine.Execute();

			// Assert
			Assert.Equal(expectedResult, machine.CPU.A);
			Assert.Equal(expectedFlags, machine.CPU.F);
		}

		[Fact]
		public void ANDHL()
		{
			// Arrange
			byte[] code = new byte[] { 0xA6 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				HL = 0x1000,
				AF = 0x5501
			};
			machine.SetCPUState(initState);
			machine.Memory.LoadData(new byte[] { 0xAB }, 0x1000, false);

			// Act
			machine.Execute();

			// Assert
			Assert.Equal(0x01, machine.CPU.A);
			Assert.Equal(ProcessorFlags.HalfCarry, machine.CPU.F);
		}

		[Theory]
		[InlineData(0xA8, 0xC7, ProcessorFlags.Sign)]												 // 0xA8 is XOR B
		[InlineData(0xA9, 0x27, ProcessorFlags.ParityOverflow)]										 // 0xA9 is XOR C
		[InlineData(0xAC, 0x00, ProcessorFlags.Zero | ProcessorFlags.ParityOverflow)]				 // 0xAC is XOR H
		[InlineData(0xAD, 0xA7, ProcessorFlags.Sign)]												 // 0xAD is XOR L
		public void XORRegister_Register(byte instruction, byte expectedResult, ProcessorFlags expectedFlags)
		{
			// Arrange
			byte[] code = new byte[] { instruction };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				BC = 0x6080,
				HL = 0xA700,
				AF = 0xA700
			};
			machine.SetCPUState(initState);

			// Act
			machine.Execute();

			// Assert
			Assert.Equal(expectedResult, machine.CPU.A);
			Assert.Equal(expectedFlags, machine.CPU.F);
		}

		[Theory]
		[InlineData(new byte[] { 0xEE, 0x40 }, 0xEA, ProcessorFlags.Sign)]
		[InlineData(new byte[] { 0xEE, 0x77 }, 0xDD, ProcessorFlags.Sign | ProcessorFlags.ParityOverflow)]
		[InlineData(new byte[] { 0xEE, 0x80 }, 0x2A, (ProcessorFlags)0)]
		[InlineData(new byte[] { 0xEE, 0xAA }, 0x00, ProcessorFlags.Zero | ProcessorFlags.ParityOverflow)]
		public void XORRegister_Immediate(byte[] code, byte expectedResult, ProcessorFlags expectedFlags)
		{
			// Arrange
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				AF = 0xAA01
			};
			machine.SetCPUState(initState);

			// Act
			machine.Execute();

			// Assert
			Assert.Equal(expectedResult, machine.CPU.A);
			Assert.Equal(expectedFlags, machine.CPU.F);
		}

		[Fact]
		public void XORHL()
		{
			// Arrange
			byte[] code = new byte[] { 0xAE };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				HL = 0x1000,
				AF = 0x5501
			};
			machine.SetCPUState(initState);
			machine.Memory.LoadData(new byte[] { 0xAB }, 0x1000, false);

			// Act
			machine.Execute();

			// Assert
			Assert.Equal(0xFE, machine.CPU.A);
			Assert.Equal(ProcessorFlags.Sign, machine.CPU.F);
		}

		[Theory]
		[InlineData(0xB0, 0x6A, ProcessorFlags.ParityOverflow)]                                      // 0xB0 is OR B
		[InlineData(0xB1, 0x8B, ProcessorFlags.Sign | ProcessorFlags.ParityOverflow)]                // 0xB1 is OR C
		[InlineData(0xB4, 0xAF, ProcessorFlags.Sign | ProcessorFlags.ParityOverflow)]                // 0xB4 is OR H
		[InlineData(0xB5, 0x0A, ProcessorFlags.ParityOverflow)]										 // 0xB5 is OR L
		public void ORRegister_Register(byte instruction, byte expectedResult, ProcessorFlags expectedFlags)
		{
			// Arrange
			byte[] code = new byte[] { instruction };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				BC = 0x6081,
				HL = 0xA700,
				AF = 0x0A00
			};
			machine.SetCPUState(initState);

			// Act
			machine.Execute();

			// Assert
			Assert.Equal(expectedResult, machine.CPU.A);
			Assert.Equal(expectedFlags, machine.CPU.F);
		}

		[Theory]
		[InlineData(new byte[] { 0xF6, 0x04 }, 0x5E, (ProcessorFlags)0)]
		[InlineData(new byte[] { 0xF6, 0x77 }, 0x7F, (ProcessorFlags)0)]
		[InlineData(new byte[] { 0xF6, 0x80 }, 0xDA, ProcessorFlags.Sign)]
		[InlineData(new byte[] { 0xF6, 0x81 }, 0xDB, ProcessorFlags.Sign | ProcessorFlags.ParityOverflow)]
		public void ORRegister_Immediate(byte[] code, byte expectedResult, ProcessorFlags expectedFlags)
		{
			// Arrange
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				AF = 0x5A00
			};
			machine.SetCPUState(initState);

			// Act
			machine.Execute();

			// Assert
			Assert.Equal(expectedResult, machine.CPU.A);
			Assert.Equal(expectedFlags, machine.CPU.F);
		}

		[Fact]
		public void ORHL()
		{
			// Arrange
			byte[] code = new byte[] { 0xB6 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				HL = 0x1000,
				AF = 0x5501
			};
			machine.SetCPUState(initState);
			machine.Memory.LoadData(new byte[] { 0xA0 }, 0x1000, false);

			// Act
			machine.Execute();

			// Assert
			Assert.Equal(0xF5, machine.CPU.A);
			Assert.Equal(ProcessorFlags.Sign | ProcessorFlags.ParityOverflow, machine.CPU.F);
		}

		[Theory]
		[InlineData(0xB8, ProcessorFlags.Subtract)]												// 0xB8 is CP B
		[InlineData(0xB9, ProcessorFlags.Carry | ProcessorFlags.Subtract | ProcessorFlags.Sign | ProcessorFlags.ParityOverflow)]      // 0xB9 is CP C
		[InlineData(0xBC, ProcessorFlags.Subtract)]												// 0xBC is CP H
		[InlineData(0xBD, ProcessorFlags.Zero | ProcessorFlags.Subtract)]                       // 0xBD is CP L
		public void CPRegister_Register(byte instruction, ProcessorFlags expectedFlags)
		{
			// Arrange
			byte[] code = new byte[] { instruction };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				BC = 0x6081,
				HL = 0x007A,
				AF = 0x7A00
			};
			machine.SetCPUState(initState);

			// Act
			machine.Execute();

			// Assert
			Assert.Equal(expectedFlags, machine.CPU.F);
		}

		[Theory]
		[InlineData(new byte[] { 0xFE, 0x04 }, ProcessorFlags.Subtract)]
		[InlineData(new byte[] { 0xFE, 0x77 }, ProcessorFlags.Sign | ProcessorFlags.Carry | ProcessorFlags.Subtract)]
		[InlineData(new byte[] { 0xFE, 0x80 }, ProcessorFlags.Sign | ProcessorFlags.Subtract | ProcessorFlags.Carry | ProcessorFlags.ParityOverflow)]
		[InlineData(new byte[] { 0xFE, 0x5A }, ProcessorFlags.Zero | ProcessorFlags.Subtract)]
		public void CPRegister_Immediate(byte[] code, ProcessorFlags expectedFlags)
		{
			// Arrange
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				AF = 0x5A00
			};
			machine.SetCPUState(initState);

			// Act
			machine.Execute();

			// Assert
			Assert.Equal(expectedFlags, machine.CPU.F);
		}

		[Fact]
		public void CPHL()
		{
			// Arrange
			byte[] code = new byte[] { 0xBE };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				HL = 0x1000,
				AF = 0x5501
			};
			machine.SetCPUState(initState);
			machine.Memory.LoadData(new byte[] { 0xA0 }, 0x1000, false);

			// Act
			machine.Execute();

			// Assert
			Assert.Equal(ProcessorFlags.Sign | ProcessorFlags.ParityOverflow | ProcessorFlags.Subtract | ProcessorFlags.Carry, machine.CPU.F);
		}

		[Fact]
		public void INCRegister()
		{
			// Arrange
			byte[] code = new byte[] { 0x04 };			// 0x04 is INC B
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				BC = 0x7F00,
			};
			machine.SetCPUState(initState);

			// Act
			machine.Execute();

			// Assert
			Assert.Equal(0x80, machine.CPU.B);
			Assert.Equal(ProcessorFlags.Sign | ProcessorFlags.ParityOverflow | ProcessorFlags.HalfCarry, machine.CPU.F);
		}

		[Fact]
		public void DECRegister()
		{
			// Arrange
			byte[] code = new byte[] { 0x25 };          // 0x25 is DEC H
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				HL = 0x4000,
			};
			machine.SetCPUState(initState);

			// Act
			machine.Execute();

			// Assert
			Assert.Equal(0x3F, machine.CPU.H);
			Assert.Equal(ProcessorFlags.Subtract | ProcessorFlags.HalfCarry, machine.CPU.F);
		}

		[Fact]
		public void INCRegisterPair()
		{
			// Arrange
			byte[] code = new byte[] { 0x13 };          // 0x13 is INC DE
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				DE = 0x01FF,
			};
			machine.SetCPUState(initState);

			// Act
			machine.Execute();

			// Assert
			Assert.Equal(0x0200, machine.CPU.DE);
			Assert.Equal((ProcessorFlags)0, machine.CPU.F);
		}

		[Fact]
		public void DECRegisterPair()
		{
			// Arrange
			byte[] code = new byte[] { 0x2B };          // 0x2B is DEC HL
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				HL = 0x0001,
			};
			machine.SetCPUState(initState);

			// Act
			machine.Execute();

			// Assert
			Assert.Equal(0x0000, machine.CPU.HL);
			Assert.Equal((ProcessorFlags)0, machine.CPU.F);
		}

		[Theory]
		[InlineData(0xC0, 0x0000, 0x1234, 0x2000)]		// NZ with Zero flag clear
		[InlineData(0xC0, 0x0040, 0x0201, 0x1FFE)]      // NZ with Zero flag set
		[InlineData(0xC8, 0x0040, 0x1234, 0x2000)]      // Z with Zero flag set
		[InlineData(0xC8, 0x0000, 0x0201, 0x1FFE)]      // Z with Zero flag clear
		[InlineData(0xD0, 0x0000, 0x1234, 0x2000)]      // NC with Carry flag clear
		[InlineData(0xD0, 0x0001, 0x0201, 0x1FFE)]      // NC with Carry flag set
		[InlineData(0xD8, 0x0001, 0x1234, 0x2000)]      // C with Carry flag set
		[InlineData(0xD8, 0x0000, 0x0201, 0x1FFE)]      // C with Carry flag clear
		[InlineData(0xE0, 0x0000, 0x1234, 0x2000)]      // PO with Parity flag clear
		[InlineData(0xE0, 0x0004, 0x0201, 0x1FFE)]      // PO with Parity flag set
		[InlineData(0xE8, 0x0004, 0x1234, 0x2000)]      // PE with Parity flag set
		[InlineData(0xE8, 0x0000, 0x0201, 0x1FFE)]      // PE with Parity flag clear
		[InlineData(0xF0, 0x0000, 0x1234, 0x2000)]      // P with Sign flag clear
		[InlineData(0xF0, 0x0080, 0x0201, 0x1FFE)]      // P with Sign flag set
		[InlineData(0xF8, 0x0080, 0x1234, 0x2000)]      // M with Sign flag set
		[InlineData(0xF8, 0x0000, 0x0201, 0x1FFE)]      // M with Sign flag clear
		public void RETCondition(byte instruction, ushort initialAF, ushort expectedPC, ushort expectedSP)
		{
			// Arrange
			byte[] code = new byte[] { instruction };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				AF = initialAF,
				SP = 0x2000
			};
			machine.SetCPUState(initState);
			machine.Stack.Push(0x1234);

			// Act
			machine.Execute();

			// Assert
			Assert.Equal(expectedPC, machine.CPU.PC);
			Assert.Equal(expectedSP, machine.CPU.SP);
		}

		[Theory]
		[InlineData(0xC2, 0x0000, 0x1234)]      // NZ with Zero flag clear
		[InlineData(0xC2, 0x0040, 0x0203)]      // NZ with Zero flag set
		[InlineData(0xCA, 0x0040, 0x1234)]      // Z with Zero flag set
		[InlineData(0xCA, 0x0000, 0x0203)]      // Z with Zero flag clear
		[InlineData(0xD2, 0x0000, 0x1234)]      // NC with Carry flag clear
		[InlineData(0xD2, 0x0001, 0x0203)]      // NC with Carry flag set
		[InlineData(0xDA, 0x0001, 0x1234)]      // C with Carry flag set
		[InlineData(0xDA, 0x0000, 0x0203)]      // C with Carry flag clear
		[InlineData(0xE2, 0x0000, 0x1234)]      // PO with Parity flag clear
		[InlineData(0xE2, 0x0004, 0x0203)]      // PO with Parity flag set
		[InlineData(0xEA, 0x0004, 0x1234)]      // PE with Parity flag set
		[InlineData(0xEA, 0x0000, 0x0203)]      // PE with Parity flag clear
		[InlineData(0xF2, 0x0000, 0x1234)]      // P with Sign flag clear
		[InlineData(0xF2, 0x0080, 0x0203)]      // P with Sign flag set
		[InlineData(0xFA, 0x0080, 0x1234)]      // M with Sign flag set
		[InlineData(0xFA, 0x0000, 0x0203)]      // M with Sign flag clear
		public void JPCondition(byte instruction, ushort initialAF, ushort expectedPC)
		{
			// Arrange
			byte[] code = new byte[] { instruction, 0x34, 0x12 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				AF = initialAF
			};
			machine.SetCPUState(initState);

			// Act
			machine.Execute();

			// Assert
			Assert.Equal(expectedPC, machine.CPU.PC);
		}

		[Theory]
		[InlineData(0xC4, 0x0000, 0x1234, 0x1FFE)]      // NZ with Zero flag clear
		[InlineData(0xC4, 0x0040, 0x0203, 0x2000)]      // NZ with Zero flag set
		[InlineData(0xCC, 0x0040, 0x1234, 0x1FFE)]      // Z with Zero flag set
		[InlineData(0xCC, 0x0000, 0x0203, 0x2000)]      // Z with Zero flag clear
		[InlineData(0xD4, 0x0000, 0x1234, 0x1FFE)]      // NC with Carry flag clear
		[InlineData(0xD4, 0x0001, 0x0203, 0x2000)]      // NC with Carry flag set
		[InlineData(0xDC, 0x0001, 0x1234, 0x1FFE)]      // C with Carry flag set
		[InlineData(0xDC, 0x0000, 0x0203, 0x2000)]      // C with Carry flag clear
		[InlineData(0xE4, 0x0000, 0x1234, 0x1FFE)]      // PO with Parity flag clear
		[InlineData(0xE4, 0x0004, 0x0203, 0x2000)]      // PO with Parity flag set
		[InlineData(0xEC, 0x0004, 0x1234, 0x1FFE)]      // PE with Parity flag set
		[InlineData(0xEC, 0x0000, 0x0203, 0x2000)]      // PE with Parity flag clear
		[InlineData(0xF4, 0x0000, 0x1234, 0x1FFE)]      // P with Sign flag clear
		[InlineData(0xF4, 0x0080, 0x0203, 0x2000)]      // P with Sign flag set
		[InlineData(0xFC, 0x0080, 0x1234, 0x1FFE)]      // M with Sign flag set
		[InlineData(0xFC, 0x0000, 0x0203, 0x2000)]      // M with Sign flag clear
		public void CALLCondition(byte instruction, ushort initialAF, ushort expectedPC, ushort expectedSP)
		{
			// Arrange
			byte[] code = new byte[] { instruction, 0x34, 0x12 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				AF = initialAF,
				SP = 0x2000
			};
			machine.SetCPUState(initState);

			// Act
			machine.Execute();

			// Assert
			Assert.Equal(expectedPC, machine.CPU.PC);
			Assert.Equal(expectedSP, machine.CPU.SP);
		}

		[Fact]
		public void POPRegisterPair()
		{
			// Arrange
			byte[] code = new byte[] { 0xC1, 0xD1, 0xE1, 0xF1 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				SP = 0x2000,
			};
			machine.SetCPUState(initState);
			byte[] data = new byte[] { 0x34, 0x12, 0x45, 0x23, 0x56, 0x34, 0x67, 0x45 };
			machine.LoadData(data, 0x2000, false);

			// Act
			machine.Execute();

			// Assert
			Assert.Equal(0x1234, machine.CPU.BC);
			Assert.Equal(0x2345, machine.CPU.DE);
			Assert.Equal(0x3456, machine.CPU.HL);
			Assert.Equal(0x4567, machine.CPU.AF);
		}

		[Fact]
		public void JP()
		{
			// Arrange
			byte[] code = new byte[] { 0xC3, 0x50, 0x40 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);

			// Act
			machine.Execute();

			// Assert
			Assert.Equal(0x4050, machine.CPU.PC);
		}

		[Fact]
		public void PUSHRegisterPair()
		{
			// Arrange
			byte[] code = new byte[] { 0xC5, 0xD5, 0xE5, 0xF5 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				BC = 0x1234,
				DE = 0x2345,
				HL = 0x3456,
				AF = 0x4567,
				SP = 0x2000,
			};
			machine.SetCPUState(initState);

			// Act
			machine.Execute();

			// Assert
			Assert.Equal(0x12, machine.Memory.Data[0x1FFF]);
			Assert.Equal(0x34, machine.Memory.Data[0x1FFE]);
			Assert.Equal(0x23, machine.Memory.Data[0x1FFD]);
			Assert.Equal(0x45, machine.Memory.Data[0x1FFC]);
			Assert.Equal(0x34, machine.Memory.Data[0x1FFB]);
			Assert.Equal(0x56, machine.Memory.Data[0x1FFA]);
			Assert.Equal(0x45, machine.Memory.Data[0x1FF9]);
			Assert.Equal(0x67, machine.Memory.Data[0x1FF8]);
			Assert.Equal(0x1FF8, machine.CPU.SP);
		}

		[Fact]
		public void RET()
		{
			// Arrange
			byte[] code = new byte[] { 0xC9 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				SP = 0x2000
			};
			machine.SetCPUState(initState);
			machine.Stack.Push(0x1234);

			// Act
			machine.Execute();

			// Assert
			Assert.Equal(0x1234, machine.CPU.PC);
			Assert.Equal(0x2000, machine.CPU.SP);
		}

		[Fact]
		public void CALL()
		{
			// Arrange
			byte[] code = new byte[] { 0xCD, 0x34, 0x12 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				SP = 0x2000
			};
			machine.SetCPUState(initState);

			// Act
			machine.Execute();

			// Assert
			Assert.Equal(0x1234, machine.CPU.PC);
			Assert.Equal(0x1FFE, machine.CPU.SP);
		}

		[Fact]
		public void EXX()
		{
			// Arrange
			byte[] code = new byte[] { 0xD9 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				BC = 0x1234,
				DE = 0x2345,
				HL = 0x3456,
				AF = 0x4567,
				BC_Shadow = 0x4321,
				DE_Shadow = 0x5432,
				HL_Shadow = 0x6543,
				AF_Shadow = 0x7654
			};
			machine.SetCPUState(initState);

			// Act
			machine.Execute();

			// Assert
			Assert.Equal(0x4321, machine.CPU.BC);
			Assert.Equal(0x5432, machine.CPU.DE);
			Assert.Equal(0x6543, machine.CPU.HL);
			Assert.Equal(0x4567, machine.CPU.AF);				// AF should be unaffected
			Assert.Equal(0x1234, machine.CPU.BC_Shadow);
			Assert.Equal(0x2345, machine.CPU.DE_Shadow);
			Assert.Equal(0x3456, machine.CPU.HL_Shadow);
			Assert.Equal(0x7654, machine.CPU.AF_Shadow);		// AF' should be unaffected
		}

		[Fact]
		public void EXSPHL()
		{
			// Arrange
			byte[] code = new byte[] { 0xE3 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				HL = 0x3456,
				SP = 0x2000
			};
			machine.SetCPUState(initState);
			machine.Stack.Push(0x1234);

			// Act
			machine.Execute();

			// Assert
			Assert.Equal(0x1234, machine.CPU.HL);
			Assert.Equal(0x1FFE, machine.CPU.SP);
			Assert.Equal(0x34, machine.Memory.Data[0x1FFF]);
			Assert.Equal(0x56, machine.Memory.Data[0x1FFE]);
		}

		[Fact]
		public void JPHL()
		{
			// Arrange
			byte[] code = new byte[] { 0xE9 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				HL = 0x3456,
			};
			machine.SetCPUState(initState);

			// Act
			machine.Execute();

			// Assert
			Assert.Equal(0x3456, machine.CPU.PC);
		}

		[Fact]
		public void EXDEHL()
		{
			// Arrange
			byte[] code = new byte[] { 0xEB };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				DE = 0x1234,
				HL = 0x3456
			};
			machine.SetCPUState(initState);

			// Act
			machine.Execute();

			// Assert
			Assert.Equal(0x3456, machine.CPU.DE);
			Assert.Equal(0x1234, machine.CPU.HL);
		}

		[Fact]
		public void LDSPHL()
		{
			// Arrange
			byte[] code = new byte[] { 0xF9 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				HL = 0x3456
			};
			machine.SetCPUState(initState);

			// Act
			machine.Execute();

			// Assert
			Assert.Equal(0x3456, machine.CPU.SP);
		}

		[Fact]
		public void DI()
		{
			// Arrange
			byte[] code = new byte[] { 0xF3 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				IFF1 = true,
				IFF2 = true
			};
			machine.SetCPUState(initState);

			// Act
			machine.Execute();

			// Assert
			Assert.False(machine.CPU.IFF1);
			Assert.False(machine.CPU.IFF2);
		}

		[Fact]
		public void EI()
		{
			// Arrange
			byte[] code = new byte[] { 0xFB };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				IFF1 = false,
				IFF2 = false
			};
			machine.SetCPUState(initState);

			// Act
			machine.Execute();

			// Assert
			Assert.True(machine.CPU.IFF1);
			Assert.True(machine.CPU.IFF2);
		}

		[Fact]
		public void LDRegisterPair()
		{
			// Arrange
			byte[] code = new byte[] { 0x01, 0x00, 0x10, 0x11, 0x00, 0x20, 0x21, 0x00, 0x30, 0x31, 0x00, 0x40 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);

			// Act
			machine.Execute();

			// Assert
			Assert.Equal(0x1000, machine.CPU.BC);
			Assert.Equal(0x2000, machine.CPU.DE);
			Assert.Equal(0x3000, machine.CPU.HL);
			Assert.Equal(0x4000, machine.CPU.SP);
		}

		[Fact]
		public void LDRegisterPairAcc()
		{
			// Arrange
			byte[] code = new byte[] { 0x02, 0x12 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				BC = 0x1800,
				DE = 0x2000,
				AF = 0xAA00
			};
			machine.SetCPUState(initState);

			// Act
			machine.Execute();

			// Assert
			Assert.Equal(0xAA, machine.Memory.Data[0x1800]);
			Assert.Equal(0xAA, machine.Memory.Data[0x2000]);
		}

		[Fact]
		public void LDAddressAcc()
		{
			// Arrange
			byte[] code = new byte[] { 0x32, 0x34, 0x12 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				AF = 0x5500
			};
			machine.SetCPUState(initState);

			// Act
			machine.Execute();

			// Assert
			Assert.Equal(0x55, machine.Memory.Data[0x1234]);
		}

		[Theory]
		[InlineData(0x08, 0x10, (ProcessorFlags)0)]
		[InlineData(0x44, 0x88, (ProcessorFlags)0)]
		[InlineData(0x80, 0x01, ProcessorFlags.Carry)]
		[InlineData(0xAA, 0x55, ProcessorFlags.Carry)]
		public void RLCA(byte accValue, byte expectedResult, ProcessorFlags expectedFlags)
		{
			// Arrange
			Machine machine = new Machine();
			machine.LoadExecutableData(new byte[] { 0x07 }, 0x0200);
			CPUState initState = new CPUState
			{
				AF = (ushort)(accValue << 8 | 0x12)                // 0x12 sets Half Carry and Subtract (to check that RLCA clears them)
			};
			machine.SetCPUState(initState);

			// Act
			machine.Execute();

			// Assert
			Assert.Equal(expectedResult, machine.CPU.A);
			Assert.Equal(expectedFlags, machine.CPU.F);
		}

		[Theory]
		[InlineData(0x08, 0x04, (ProcessorFlags)0)]
		[InlineData(0x44, 0x22, (ProcessorFlags)0)]
		[InlineData(0x81, 0xC0, ProcessorFlags.Carry)]
		[InlineData(0x55, 0xAA, ProcessorFlags.Carry)]
		public void RRCA(byte accValue, byte expectedResult, ProcessorFlags expectedFlags)
		{
			// Arrange
			Machine machine = new Machine();
			machine.LoadExecutableData(new byte[] { 0x0F }, 0x0200);
			CPUState initState = new CPUState
			{
				AF = (ushort)(accValue << 8 | 0x12)             // 0x12 sets Half Carry and Subtract (to check that RLCA clears them)
			};
			machine.SetCPUState(initState);

			// Act
			machine.Execute();

			// Assert
			Assert.Equal(expectedResult, machine.CPU.A);
			Assert.Equal(expectedFlags, machine.CPU.F);
		}

		[Fact]
		public void EXAF()
		{
			// Arrange
			byte[] code = new byte[] { 0x08 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				BC = 0x1234,
				DE = 0x2345,
				HL = 0x3456,
				AF = 0x4567,
				BC_Shadow = 0x4321,
				DE_Shadow = 0x5432,
				HL_Shadow = 0x6543,
				AF_Shadow = 0x7654
			};
			machine.SetCPUState(initState);

			// Act
			machine.Execute();

			// Assert
			Assert.Equal(0x1234, machine.CPU.BC);               // BC should be unaffected
			Assert.Equal(0x2345, machine.CPU.DE);               // DE should be unaffected
			Assert.Equal(0x3456, machine.CPU.HL);               // HL should be unaffected
			Assert.Equal(0x7654, machine.CPU.AF);
			Assert.Equal(0x4321, machine.CPU.BC_Shadow);        // BC' should be unaffected
			Assert.Equal(0x5432, machine.CPU.DE_Shadow);        // DE' should be unaffected
			Assert.Equal(0x6543, machine.CPU.HL_Shadow);        // HL' should be unaffected
			Assert.Equal(0x4567, machine.CPU.AF_Shadow);
		}

		[Theory]
		[InlineData(0x0A, 0x55)]
		[InlineData(0x1A, 0xAA)]
		public void LDAccRegisterPair(byte instruction, byte expectedResult)
		{
			// Arrange
			Machine machine = new Machine();
			machine.LoadExecutableData(new byte[] { instruction }, 0x0200);
			CPUState initState = new CPUState
			{
				BC = 0x1800,
				DE = 0x2000
			};
			machine.SetCPUState(initState);
			machine.Memory.Data[0x1800] = 0x55;
			machine.Memory.Data[0x2000] = 0xAA;

			// Act
			machine.Execute();

			// Assert
			Assert.Equal(expectedResult, machine.CPU.A);
		}

		[Fact]
		public void LDAccAddress()
		{
			// Arrange
			byte[] code = new byte[] { 0x3A, 0x00, 0x18 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			machine.Memory.Data[0x1800] = 0x55;

			// Act
			machine.Execute();

			// Assert
			Assert.Equal(0x55, machine.CPU.A);
		}

		[Theory]
		[InlineData(0x09, 0x4000)]
		[InlineData(0x19, 0x5000)]
		[InlineData(0x29, 0x6000)]
		[InlineData(0x39, 0x7000)]
		public void ADDHLRegisterPair(byte instruction, ushort expectedResult)
		{
			// Arrange
			Machine machine = new Machine();
			machine.LoadExecutableData(new byte[] { instruction }, 0x0200);
			CPUState initState = new CPUState
			{
				BC = 0x1000,
				DE = 0x2000,
				HL = 0x3000,
				SP = 0x4000
			};
			machine.SetCPUState(initState);

			// Act
			machine.Execute();

			// Assert
			Assert.Equal(expectedResult, machine.CPU.HL);
		}

		[Theory]
		[InlineData(0x10, 0x0F, 0x01F2)]
		[InlineData(0x01, 0x00, 0x0202)]
		public void DJNZ(byte initialB, byte expectedB, ushort expectedPC)
		{
			// Arrange
			Machine machine = new Machine();
			machine.LoadExecutableData(new byte[] { 0x10, 0xF0 }, 0x0200);
			CPUState initState = new CPUState
			{
				BC = (ushort)(initialB << 8)
			};
			machine.SetCPUState(initState);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(expectedB, machine.CPU.B);
			Assert.Equal(expectedPC, machine.CPU.PC);
		}

		[Theory]
		[InlineData(0x08, ProcessorFlags.HalfCarry | ProcessorFlags.Subtract, 0x10, (ProcessorFlags)0)]
		[InlineData(0x20, ProcessorFlags.Carry | ProcessorFlags.HalfCarry | ProcessorFlags.Subtract, 0x41, (ProcessorFlags)0)]
		[InlineData(0x80, ProcessorFlags.HalfCarry | ProcessorFlags.Subtract, 0x00, ProcessorFlags.Carry)]
		[InlineData(0x55, ProcessorFlags.Carry | ProcessorFlags.HalfCarry | ProcessorFlags.Subtract, 0xAB, (ProcessorFlags)0)]
		public void RLA(byte accValue, ProcessorFlags initFlags, byte expectedResult, ProcessorFlags expectedFlags)
		{
			// Arrange
			Machine machine = new Machine();
			machine.LoadExecutableData(new byte[] { 0x17 }, 0x0200);
			CPUState initState = new CPUState
			{
				AF = (ushort)(accValue << 8 | (byte)initFlags)
			};
			machine.SetCPUState(initState);

			// Act
			machine.Execute();

			// Assert
			Assert.Equal(expectedResult, machine.CPU.A);
			Assert.Equal(expectedFlags, machine.CPU.F);
		}

		[Theory]
		[InlineData(0x08, ProcessorFlags.HalfCarry | ProcessorFlags.Subtract, 0x04, (ProcessorFlags)0)]
		[InlineData(0x20, ProcessorFlags.Carry | ProcessorFlags.HalfCarry | ProcessorFlags.Subtract, 0x90, (ProcessorFlags)0)]
		[InlineData(0x01, ProcessorFlags.HalfCarry | ProcessorFlags.Subtract, 0x00, ProcessorFlags.Carry)]
		[InlineData(0x55, ProcessorFlags.HalfCarry | ProcessorFlags.Subtract, 0x2A, ProcessorFlags.Carry)]
		public void RRA(byte accValue, ProcessorFlags initFlags, byte expectedResult, ProcessorFlags expectedFlags)
		{
			// Arrange
			Machine machine = new Machine();
			machine.LoadExecutableData(new byte[] { 0x1F }, 0x0200);
			CPUState initState = new CPUState
			{
				AF = (ushort)(accValue << 8 | (byte)initFlags)
			};
			machine.SetCPUState(initState);

			// Act
			machine.Execute();

			// Assert
			Assert.Equal(expectedResult, machine.CPU.A);
			Assert.Equal(expectedFlags, machine.CPU.F);
		}

		[Fact]
		public void JR()
		{
			// Arrange
			byte[] code = new byte[] { 0x18, 0x20 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(0x0222, machine.CPU.PC);
		}

		[Theory]
		[InlineData(0x20, 0x0000, 0x0242)]      // NZ with Zero flag clear
		[InlineData(0x20, 0x0040, 0x0202)]      // NZ with Zero flag set
		[InlineData(0x28, 0x0040, 0x0242)]      // Z with Zero flag set
		[InlineData(0x28, 0x0000, 0x0202)]      // Z with Zero flag clear
		[InlineData(0x30, 0x0000, 0x0242)]      // NC with Carry flag clear
		[InlineData(0x30, 0x0001, 0x0202)]      // NC with Carry flag set
		[InlineData(0x38, 0x0001, 0x0242)]      // C with Carry flag set
		[InlineData(0x38, 0x0000, 0x0202)]      // C with Carry flag clear
		public void JRCondition(byte instruction, ushort initialAF, ushort expectedPC)
		{
			// Arrange
			byte[] code = new byte[] { instruction, 0x40 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				AF = initialAF
			};
			machine.SetCPUState(initState);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(expectedPC, machine.CPU.PC);
		}

		[Theory]
		[InlineData(0x3C, ProcessorFlags.HalfCarry, 0x42, ProcessorFlags.HalfCarry | ProcessorFlags.ParityOverflow)]
		[InlineData(0xCC, (ProcessorFlags)0, 0x32, ProcessorFlags.Carry | ProcessorFlags.HalfCarry)]
		[InlineData(0xEE, ProcessorFlags.Carry | ProcessorFlags.Subtract | ProcessorFlags.HalfCarry | ProcessorFlags.Sign, 0x88, ProcessorFlags.Carry | ProcessorFlags.Subtract | ProcessorFlags.ParityOverflow | ProcessorFlags.Sign)]
		[InlineData(0x12, ProcessorFlags.Subtract, 0x12, ProcessorFlags.Subtract | ProcessorFlags.ParityOverflow)]
		public void DAA(byte accValue, ProcessorFlags initFlags, byte expectedValue, ProcessorFlags expectedFlags)
		{
			// Arrange
			byte[] code = new byte[] { 0x27 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				AF = (ushort)(accValue << 8 | (byte)initFlags)
			};
			machine.SetCPUState(initState);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(expectedValue, machine.CPU.A);
			Assert.Equal(expectedFlags, machine.CPU.F);
		}

		[Fact]
		public void CPL()
		{
			// Arrange
			byte[] code = new byte[] { 0x2F };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				AF = 0x5500
			};
			machine.SetCPUState(initState);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(0xAA, machine.CPU.A);
			Assert.True(machine.CPU.HalfCarryFlag);
			Assert.True(machine.CPU.SubtractFlag);
		}

		[Theory]
		[InlineData((ProcessorFlags)0, ProcessorFlags.Carry)]
		[InlineData(ProcessorFlags.Subtract, ProcessorFlags.Carry)]
		[InlineData(ProcessorFlags.Carry, ProcessorFlags.HalfCarry)]
		[InlineData(ProcessorFlags.Subtract | ProcessorFlags.Carry, ProcessorFlags.HalfCarry)]
		public void CCF(ProcessorFlags initFlags, ProcessorFlags expectedFlags)
		{
			// Arrange
			byte[] code = new byte[] { 0x3F };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				AF = (ushort)initFlags
			};
			machine.SetCPUState(initState);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(expectedFlags, machine.CPU.F);
		}

		[Fact]
		public void SCF()
		{
			// Arrange
			byte[] code = new byte[] { 0x37 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				AF = 0x0012					// F = 0x12 is Half Carry and Subtract both set (to check that SCF clears them)
			};
			machine.SetCPUState(initState);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.True(machine.CPU.CarryFlag);
			Assert.False(machine.CPU.HalfCarryFlag);
			Assert.False(machine.CPU.SubtractFlag);
		}

		[Fact]
		public void INCHLIndirect()
		{
			// Arrange
			byte[] code = new byte[] { 0x34 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				HL = 0x2000
			};
			machine.SetCPUState(initState);
			machine.Memory.LoadData(new byte[] { 0x3F }, 0x2000, false);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(0x40, machine.Memory.Data[0x2000]);
		}

		[Fact]
		public void DECHLIndirect()
		{
			// Arrange
			byte[] code = new byte[] { 0x35 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				HL = 0x2000
			};
			machine.SetCPUState(initState);
			machine.Memory.LoadData(new byte[] { 0x3F }, 0x2000, false);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(0x3E, machine.Memory.Data[0x2000]);
		}

		[Fact]
		public void LDHLIndirectImm()
		{
			// Arrange
			byte[] code = new byte[] { 0x36, 0x7F };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				HL = 0x2000
			};
			machine.SetCPUState(initState);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(0x7F, machine.Memory.Data[0x2000]);
		}

		[Theory]
		[InlineData(0xC7, 0x0000)]
		[InlineData(0xCF, 0x0008)]
		[InlineData(0xD7, 0x0010)]
		[InlineData(0xDF, 0x0018)]
		[InlineData(0xE7, 0x0020)]
		[InlineData(0xEF, 0x0028)]
		[InlineData(0xF7, 0x0030)]
		[InlineData(0xFF, 0x0038)]
		public void RST(byte instruction, ushort expectedPC)
		{
			// Arrange
			byte[] code = new byte[] { instruction };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				SP = 0x2000
			};
			machine.SetCPUState(initState);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(expectedPC, machine.CPU.PC);
			Assert.Equal(0x1FFE, machine.CPU.SP);
		}

		[Fact]
		public void OUTAcc()
		{
			// Arrange
			byte[] code = new byte[] { 0xD3, 0x30 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				AF = 0x1200
			};
			machine.SetCPUState(initState);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(0x12, ((DummyPort)machine.Port).DummyData[0x30]);
		}

		[Fact]
		public void INAcc()
		{
			// Arrange
			byte[] code = new byte[] { 0xDB, 0x50 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(0xAF, machine.CPU.A);
		}

		[Fact]
		public void LDAddressHL()
		{
			// Arrange
			byte[] code = new byte[] { 0x22, 0x00, 0x20 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				HL = 0x1234
			};
			machine.SetCPUState(initState);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(0x34, machine.Memory.Data[0x2000]);
			Assert.Equal(0x12, machine.Memory.Data[0x2001]);
		}

		[Fact]
		public void LDHLAddress()
		{
			// Arrange
			byte[] code = new byte[] { 0x2A, 0x00, 0x20 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			machine.Memory.LoadData(new byte[] { 0x3F, 0x2A }, 0x2000, false);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(0x2A3F, machine.CPU.HL);
		}

		// ************************
		//
		// CB-prefixed instructions
		//
		// ************************

		[Theory]
		[InlineData(0x04, 0x08, (ProcessorFlags)0)]
		[InlineData(0x80, 0x01, ProcessorFlags.Carry)]
		[InlineData(0xAA, 0x55, ProcessorFlags.Carry | ProcessorFlags.ParityOverflow)]
		[InlineData(0x40, 0x80, ProcessorFlags.Sign)]
		[InlineData(0x00, 0x00, ProcessorFlags.Zero | ProcessorFlags.ParityOverflow)]
		public void RLCRegister(byte initValue, byte expectedResult, ProcessorFlags expectedFlags)
		{
			// Arrange
			byte[] code = new byte[] { 0xCB, 0x00 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				BC = (ushort)(initValue << 8)
			};
			machine.SetCPUState(initState);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(expectedResult, machine.CPU.B);
			Assert.Equal(expectedFlags, machine.CPU.F);
		}

		[Theory]
		[InlineData(0x04, 0x08, (ProcessorFlags)0)]
		[InlineData(0x80, 0x01, ProcessorFlags.Carry)]
		[InlineData(0xAA, 0x55, ProcessorFlags.Carry | ProcessorFlags.ParityOverflow)]
		[InlineData(0x40, 0x80, ProcessorFlags.Sign)]
		[InlineData(0x00, 0x00, ProcessorFlags.Zero | ProcessorFlags.ParityOverflow)]
		public void RLCHLIndirect(byte initValue, byte expectedResult, ProcessorFlags expectedFlags)
		{
			// Arrange
			byte[] code = new byte[] { 0xCB, 0x06 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				HL = 0x2000
			};
			machine.SetCPUState(initState);
			machine.Memory.LoadData(new byte[] { initValue }, 0x2000, false);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(expectedResult, machine.Memory.Data[0x2000]);
			Assert.Equal(expectedFlags, machine.CPU.F);
		}

		[Theory]
		[InlineData(0x44, 0x22, ProcessorFlags.ParityOverflow)]
		[InlineData(0x01, 0x80, ProcessorFlags.Carry | ProcessorFlags.Sign)]
		[InlineData(0xAA, 0x55, ProcessorFlags.ParityOverflow)]
		[InlineData(0x55, 0xAA, ProcessorFlags.Sign | ProcessorFlags.ParityOverflow | ProcessorFlags.Carry)]
		[InlineData(0x00, 0x00, ProcessorFlags.Zero | ProcessorFlags.ParityOverflow)]
		public void RRCRegister(byte initValue, byte expectedResult, ProcessorFlags expectedFlags)
		{
			// Arrange
			byte[] code = new byte[] { 0xCB, 0x09 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				BC = (ushort)initValue
			};
			machine.SetCPUState(initState);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(expectedResult, machine.CPU.C);
			Assert.Equal(expectedFlags, machine.CPU.F);
		}

		[Theory]
		[InlineData(0x44, 0x22, ProcessorFlags.ParityOverflow)]
		[InlineData(0x01, 0x80, ProcessorFlags.Carry | ProcessorFlags.Sign)]
		[InlineData(0xAA, 0x55, ProcessorFlags.ParityOverflow)]
		[InlineData(0x55, 0xAA, ProcessorFlags.Sign | ProcessorFlags.ParityOverflow | ProcessorFlags.Carry)]
		[InlineData(0x00, 0x00, ProcessorFlags.Zero | ProcessorFlags.ParityOverflow)]
		public void RRCHLIndirect(byte initValue, byte expectedResult, ProcessorFlags expectedFlags)
		{
			// Arrange
			byte[] code = new byte[] { 0xCB, 0x0E };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				HL = 0x2000
			};
			machine.SetCPUState(initState);
			machine.Memory.LoadData(new byte[] { initValue }, 0x2000, false);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(expectedResult, machine.Memory.Data[0x2000]);
			Assert.Equal(expectedFlags, machine.CPU.F);
		}

		[Theory]
		[InlineData(0x04, (ProcessorFlags)0, 0x08, (ProcessorFlags)0)]
		[InlineData(0x80, (ProcessorFlags)0, 0x00, ProcessorFlags.Carry | ProcessorFlags.Zero | ProcessorFlags.ParityOverflow)]
		[InlineData(0xAA, (ProcessorFlags)0, 0x54, ProcessorFlags.Carry)]
		[InlineData(0x40, ProcessorFlags.Carry, 0x81, ProcessorFlags.Sign | ProcessorFlags.ParityOverflow)]
		[InlineData(0x00, ProcessorFlags.Carry, 0x01, (ProcessorFlags)0)]
		public void RLRegister(byte initValue, ProcessorFlags initFlags, byte expectedResult, ProcessorFlags expectedFlags)
		{
			// Arrange
			byte[] code = new byte[] { 0xCB, 0x12 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				DE = (ushort)(initValue << 8),
				AF = (ushort)initFlags
			};
			machine.SetCPUState(initState);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(expectedResult, machine.CPU.D);
			Assert.Equal(expectedFlags, machine.CPU.F);
		}

		[Theory]
		[InlineData(0x04, (ProcessorFlags)0, 0x08, (ProcessorFlags)0)]
		[InlineData(0x80, (ProcessorFlags)0, 0x00, ProcessorFlags.Carry | ProcessorFlags.Zero | ProcessorFlags.ParityOverflow)]
		[InlineData(0xAA, (ProcessorFlags)0, 0x54, ProcessorFlags.Carry)]
		[InlineData(0x40, ProcessorFlags.Carry, 0x81, ProcessorFlags.Sign | ProcessorFlags.ParityOverflow)]
		[InlineData(0x00, ProcessorFlags.Carry, 0x01, (ProcessorFlags)0)]
		public void RLHLIndirect(byte initValue, ProcessorFlags initFlags, byte expectedResult, ProcessorFlags expectedFlags)
		{
			// Arrange
			byte[] code = new byte[] { 0xCB, 0x16 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				HL = 0x2000,
				AF = (ushort)initFlags
			};
			machine.SetCPUState(initState);
			machine.Memory.LoadData(new byte[] { initValue }, 0x2000, false);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(expectedResult, machine.Memory.Data[0x2000]);
			Assert.Equal(expectedFlags, machine.CPU.F);
		}

		[Theory]
		[InlineData(0x04, (ProcessorFlags)0, 0x02, (ProcessorFlags)0)]
		[InlineData(0x01, (ProcessorFlags)0, 0x00, ProcessorFlags.Carry | ProcessorFlags.Zero | ProcessorFlags.ParityOverflow)]
		[InlineData(0x55, (ProcessorFlags)0, 0x2A, ProcessorFlags.Carry)]
		[InlineData(0x40, ProcessorFlags.Carry, 0xA0, ProcessorFlags.Sign | ProcessorFlags.ParityOverflow)]
		[InlineData(0x00, ProcessorFlags.Carry, 0x80, ProcessorFlags.Sign)]
		public void RRRegister(byte initValue, ProcessorFlags initFlags, byte expectedResult, ProcessorFlags expectedFlags)
		{
			// Arrange
			byte[] code = new byte[] { 0xCB, 0x1B };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				DE = initValue,
				AF = (ushort)initFlags
			};
			machine.SetCPUState(initState);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(expectedResult, machine.CPU.E);
			Assert.Equal(expectedFlags, machine.CPU.F);
		}

		[Theory]
		[InlineData(0x04, (ProcessorFlags)0, 0x02, (ProcessorFlags)0)]
		[InlineData(0x01, (ProcessorFlags)0, 0x00, ProcessorFlags.Carry | ProcessorFlags.Zero | ProcessorFlags.ParityOverflow)]
		[InlineData(0x55, (ProcessorFlags)0, 0x2A, ProcessorFlags.Carry)]
		[InlineData(0x40, ProcessorFlags.Carry, 0xA0, ProcessorFlags.Sign | ProcessorFlags.ParityOverflow)]
		[InlineData(0x00, ProcessorFlags.Carry, 0x80, ProcessorFlags.Sign)]
		public void RRHLIndirect(byte initValue, ProcessorFlags initFlags, byte expectedResult, ProcessorFlags expectedFlags)
		{
			// Arrange
			byte[] code = new byte[] { 0xCB, 0x1E };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				HL = 0x2000,
				AF = (ushort)initFlags
			};
			machine.SetCPUState(initState);
			machine.Memory.LoadData(new byte[] { initValue }, 0x2000, false);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(expectedResult, machine.Memory.Data[0x2000]);
			Assert.Equal(expectedFlags, machine.CPU.F);
		}

		[Theory]
		[InlineData(0x04, 0x08, (ProcessorFlags)0)]
		[InlineData(0x80, 0x00, ProcessorFlags.Carry | ProcessorFlags.Zero | ProcessorFlags.ParityOverflow)]
		[InlineData(0xAA, 0x54, ProcessorFlags.Carry)]
		[InlineData(0x40, 0x80, ProcessorFlags.Sign)]
		[InlineData(0x00, 0x00, ProcessorFlags.Zero | ProcessorFlags.ParityOverflow)]
		public void SLARegister(byte initValue, byte expectedResult, ProcessorFlags expectedFlags)
		{
			// Arrange
			byte[] code = new byte[] { 0xCB, 0x24 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				HL = (ushort)(initValue << 8),
			};
			machine.SetCPUState(initState);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(expectedResult, machine.CPU.H);
			Assert.Equal(expectedFlags, machine.CPU.F);
		}

		[Theory]
		[InlineData(0x04, 0x08, (ProcessorFlags)0)]
		[InlineData(0x80, 0x00, ProcessorFlags.Carry | ProcessorFlags.Zero | ProcessorFlags.ParityOverflow)]
		[InlineData(0xAA, 0x54, ProcessorFlags.Carry)]
		[InlineData(0x40, 0x80, ProcessorFlags.Sign)]
		[InlineData(0x00, 0x00, ProcessorFlags.Zero | ProcessorFlags.ParityOverflow)]
		public void SLAHLIndirect(byte initValue, byte expectedResult, ProcessorFlags expectedFlags)
		{
			// Arrange
			byte[] code = new byte[] { 0xCB, 0x26 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				HL = 0x2000
			};
			machine.SetCPUState(initState);
			machine.Memory.LoadData(new byte[] { initValue }, 0x2000, false);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(expectedResult, machine.Memory.Data[0x2000]);
			Assert.Equal(expectedFlags, machine.CPU.F);
		}

		[Theory]
		[InlineData(0x04, 0x02, (ProcessorFlags)0)]
		[InlineData(0x80, 0xC0, ProcessorFlags.Sign | ProcessorFlags.ParityOverflow)]
		[InlineData(0x01, 0x00, ProcessorFlags.Carry | ProcessorFlags.Zero | ProcessorFlags.ParityOverflow)]
		[InlineData(0x55, 0x2A, ProcessorFlags.Carry)]
		[InlineData(0xAA, 0xD5, ProcessorFlags.Sign)]
		public void SRARegister(byte initValue, byte expectedResult, ProcessorFlags expectedFlags)
		{
			// Arrange
			byte[] code = new byte[] { 0xCB, 0x2D };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				HL = initValue
			};
			machine.SetCPUState(initState);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(expectedResult, machine.CPU.L);
			Assert.Equal(expectedFlags, machine.CPU.F);
		}

		[Theory]
		[InlineData(0x04, 0x02, (ProcessorFlags)0)]
		[InlineData(0x80, 0xC0, ProcessorFlags.Sign | ProcessorFlags.ParityOverflow)]
		[InlineData(0x01, 0x00, ProcessorFlags.Carry | ProcessorFlags.Zero | ProcessorFlags.ParityOverflow)]
		[InlineData(0x55, 0x2A, ProcessorFlags.Carry)]
		[InlineData(0xAA, 0xD5, ProcessorFlags.Sign)]
		public void SRAHLIndirect(byte initValue, byte expectedResult, ProcessorFlags expectedFlags)
		{
			// Arrange
			byte[] code = new byte[] { 0xCB, 0x2E };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				HL = 0x2000
			};
			machine.SetCPUState(initState);
			machine.Memory.LoadData(new byte[] { initValue }, 0x2000, false);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(expectedResult, machine.Memory.Data[0x2000]);
			Assert.Equal(expectedFlags, machine.CPU.F);
		}

		[Theory]
		[InlineData(0x04, 0x02, (ProcessorFlags)0)]
		[InlineData(0x81, 0x40, ProcessorFlags.Carry)]
		[InlineData(0x01, 0x00, ProcessorFlags.Carry | ProcessorFlags.Zero | ProcessorFlags.ParityOverflow)]
		[InlineData(0x55, 0x2A, ProcessorFlags.Carry)]
		[InlineData(0xAA, 0x55, ProcessorFlags.ParityOverflow)]
		public void SRLRegister(byte initValue, byte expectedResult, ProcessorFlags expectedFlags)
		{
			// Arrange
			byte[] code = new byte[] { 0xCB, 0x3F };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				AF = (ushort)(initValue << 8)
			};
			machine.SetCPUState(initState);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(expectedResult, machine.CPU.A);
			Assert.Equal(expectedFlags, machine.CPU.F);
		}

		[Theory]
		[InlineData(0x04, 0x02, (ProcessorFlags)0)]
		[InlineData(0x81, 0x40, ProcessorFlags.Carry)]
		[InlineData(0x01, 0x00, ProcessorFlags.Carry | ProcessorFlags.Zero | ProcessorFlags.ParityOverflow)]
		[InlineData(0x55, 0x2A, ProcessorFlags.Carry)]
		[InlineData(0xAA, 0x55, ProcessorFlags.ParityOverflow)]
		public void SRLHLIndirect(byte initValue, byte expectedResult, ProcessorFlags expectedFlags)
		{
			// Arrange
			byte[] code = new byte[] { 0xCB, 0x3E };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				HL = 0x2000
			};
			machine.SetCPUState(initState);
			machine.Memory.LoadData(new byte[] { initValue }, 0x2000, false);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(expectedResult, machine.Memory.Data[0x2000]);
			Assert.Equal(expectedFlags, machine.CPU.F);
		}

		[Theory]
		[InlineData(0x48, ProcessorFlags.HalfCarry)]							// BIT 1,B
		[InlineData(0x41, ProcessorFlags.HalfCarry | ProcessorFlags.Zero)]		// BIT 0,C
		[InlineData(0x62, ProcessorFlags.HalfCarry)]							// BIT 4,D
		[InlineData(0x7B, ProcessorFlags.HalfCarry | ProcessorFlags.Zero)]		// BIT 7,E
		public void BITRegister(byte instruction, ProcessorFlags expectedFlags)
		{
			// Arrange
			byte[] code = new byte[] { 0xCB, instruction };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				BC = 0xAAAA,
				DE = 0x5555
			};
			machine.SetCPUState(initState);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(expectedFlags, machine.CPU.F);
		}

		[Theory]
		[InlineData(0x46, ProcessorFlags.HalfCarry | ProcessorFlags.Zero)]      // BIT 0,(HL)
		[InlineData(0x5E, ProcessorFlags.HalfCarry)]							// BIT 3,(HL)
		[InlineData(0x76, ProcessorFlags.HalfCarry)]                            // BIT 6,(HL)
		[InlineData(0x7E, ProcessorFlags.HalfCarry | ProcessorFlags.Zero)]      // BIT 7,(HL)
		public void BITHLIndirect(byte instruction, ProcessorFlags expectedFlags)
		{
			// Arrange
			byte[] code = new byte[] { 0xCB, instruction };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				HL = 0x2000
			};
			machine.SetCPUState(initState);
			machine.Memory.LoadData(new byte[] { 0x5A }, 0x2000, false);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(expectedFlags, machine.CPU.F);
		}

		[Theory]
		[InlineData(0x88, 0xA8AA)]          // RES 1,B
		[InlineData(0x81, 0xAAAA)]			// RES 0,C
		[InlineData(0xB8, 0x2AAA)]          // RES 7,B
		[InlineData(0xA9, 0xAA8A)]			// RES 5,C
		public void RESRegister(byte instruction, ushort expectedResult)
		{
			// Arrange
			byte[] code = new byte[] { 0xCB, instruction };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				BC = 0xAAAA
			};
			machine.SetCPUState(initState);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(expectedResult, machine.CPU.BC);
		}

		[Theory]
		[InlineData(0x8E, 0x58)]          // RES 1,(HL)
		[InlineData(0xA6, 0x4A)]          // RES 4,(HL)
		[InlineData(0xAE, 0x5A)]          // RES 5,(HL)
		public void RESHLIndirect(byte instruction, byte expectedResult)
		{
			// Arrange
			byte[] code = new byte[] { 0xCB, instruction };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				HL = 0x2000
			};
			machine.SetCPUState(initState);
			machine.Memory.LoadData(new byte[] { 0x5A }, 0x2000, false);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(expectedResult, machine.Memory.Data[0x2000]);
		}


		[Theory]
		[InlineData(0xCB, 0xAA57)]          // SET 1,E
		[InlineData(0xE3, 0xAA55)]          // SET 4,E
		[InlineData(0xD2, 0xAE55)]          // SET 2,D
		[InlineData(0xF2, 0xEA55)]          // SET 6,D
		public void SETRegister(byte instruction, ushort expectedResult)
		{
			// Arrange
			byte[] code = new byte[] { 0xCB, instruction };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				DE = 0xAA55
			};
			machine.SetCPUState(initState);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(expectedResult, machine.CPU.DE);
		}

		[Theory]
		[InlineData(0xC6, 0x5B)]          // SET 0,(HL)
		[InlineData(0xDE, 0x5A)]          // SET 3,(HL)
		[InlineData(0xFE, 0xDA)]          // SET 7,(HL)
		public void SETHLIndirect(byte instruction, ushort expectedResult)
		{
			// Arrange
			byte[] code = new byte[] { 0xCB, instruction };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				HL = 0x2000
			};
			machine.SetCPUState(initState);
			machine.Memory.LoadData(new byte[] { 0x5A }, 0x2000, false);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(expectedResult, machine.Memory.Data[0x2000]);
		}


		// ************************
		//
		// ED-prefixed instructions
		//
		// ************************

		[Fact]
		public void INRegister()
		{
			// Arrange
			byte[] code = new byte[] { 0xED, 0x50 };				// IN D,(C)
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				BC = 0x0060
			};
			machine.SetCPUState(initState);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(0x9F, machine.CPU.D);
		}

		[Fact]
		public void OUTRegister()
		{
			// Arrange
			byte[] code = new byte[] { 0xED, 0x69 };                // OUT (C),L
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				BC = 0x0080,
				HL = 0x1234
			};
			machine.SetCPUState(initState);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(0x34, ((DummyPort)machine.Port).DummyData[0x80]);
		}

		[Theory]
		[InlineData(0x42, (ProcessorFlags)0, 0x0DCC, ProcessorFlags.Subtract | ProcessorFlags.HalfCarry)]          // SBC HL,BC
		[InlineData(0x42, ProcessorFlags.Carry, 0x0DCB, ProcessorFlags.Subtract | ProcessorFlags.HalfCarry)]       // SBC HL,BC
		[InlineData(0x52, (ProcessorFlags)0, 0xCAAB, ProcessorFlags.Subtract | ProcessorFlags.HalfCarry | ProcessorFlags.Carry | ProcessorFlags.Sign)]          // SBC HL,DE
		[InlineData(0x52, ProcessorFlags.Carry, 0xCAAA, ProcessorFlags.Subtract | ProcessorFlags.HalfCarry | ProcessorFlags.Carry | ProcessorFlags.Sign)]       // SBC HL,DE
		[InlineData(0x62, (ProcessorFlags)0, 0x0000, ProcessorFlags.Subtract | ProcessorFlags.Zero)]				// SBC HL,HL
		[InlineData(0x62, ProcessorFlags.Carry, 0xFFFF, ProcessorFlags.Subtract | ProcessorFlags.HalfCarry | ProcessorFlags.Carry | ProcessorFlags.Sign)]       // SBC HL,HL
		[InlineData(0x72, (ProcessorFlags)0, 0x2100, ProcessorFlags.Subtract | ProcessorFlags.Carry | ProcessorFlags.HalfCarry)]          // SBC HL,SP
		[InlineData(0x72, ProcessorFlags.Carry, 0x20FF, ProcessorFlags.Subtract | ProcessorFlags.HalfCarry | ProcessorFlags.Carry)]       // SBC HL,SP
		public void SBCHLRegisterPair(byte instruction, ProcessorFlags initFlags, ushort expectedResult, ProcessorFlags expectedFlags)
		{
			// Arrange
			byte[] code = new byte[] { 0xED, instruction };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				BC = 0x1234,
				DE = 0x5555,
				HL = 0x2000,
				SP = 0xFF00,
				AF = (ushort)initFlags
			};
			machine.SetCPUState(initState);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(expectedResult, machine.CPU.HL);
			Assert.Equal(expectedFlags, machine.CPU.F);
		}

		[Theory]
		[InlineData(0x4A, (ProcessorFlags)0, 0x3234, (ProcessorFlags)0)]          // ADC HL,BC
		[InlineData(0x4A, ProcessorFlags.Carry, 0x3235, (ProcessorFlags)0)]       // ADC HL,BC
		[InlineData(0x5A, (ProcessorFlags)0, 0x9777, ProcessorFlags.ParityOverflow | ProcessorFlags.Sign)]          // ADC HL,DE
		[InlineData(0x5A, ProcessorFlags.Carry, 0x9778, ProcessorFlags.ParityOverflow | ProcessorFlags.Sign)]       // ADC HL,DE
		[InlineData(0x6A, (ProcessorFlags)0, 0x4000, (ProcessorFlags)0)]             // ADC HL,HL
		[InlineData(0x6A, ProcessorFlags.Carry, 0x4001, (ProcessorFlags)0)]			 // ADC HL,HL
		[InlineData(0x7A, (ProcessorFlags)0, 0x1F00, ProcessorFlags.Carry)]          // ADC HL,SP
		[InlineData(0x7A, ProcessorFlags.Carry, 0x1F01, ProcessorFlags.Carry)]       // ADC HL,SP
		public void ADCHLRegisterPair(byte instruction, ProcessorFlags initFlags, ushort expectedResult, ProcessorFlags expectedFlags)
		{
			// Arrange
			byte[] code = new byte[] { 0xED, instruction };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				BC = 0x1234,
				DE = 0x7777,
				HL = 0x2000,
				SP = 0xFF00,
				AF = (ushort)initFlags
			};
			machine.SetCPUState(initState);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(expectedResult, machine.CPU.HL);
			Assert.Equal(expectedFlags, machine.CPU.F);
		}

		[Fact]
		public void LDAddressRegisterPair()
		{
			// Arrange
			byte[] code = new byte[] { 0xED, 0x4B, 0x00, 0x20, 0xED, 0x5B, 0x02, 0x20, 0xED, 0x7B, 0x04, 0x20 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			machine.Memory.LoadData(new byte[] { 0x34, 0x12, 0x88, 0x77, 0x00, 0xFF }, 0x2000, false);

			// Act
			machine.Execute();

			// Assert
			Assert.Equal(0x1234, machine.CPU.BC);
			Assert.Equal(0x7788, machine.CPU.DE);
			Assert.Equal(0xFF00, machine.CPU.SP);
		}

		[Theory]
		[InlineData(0x10, 0xF0, ProcessorFlags.Sign | ProcessorFlags.Carry | ProcessorFlags.Subtract)]
		[InlineData(0x7F, 0x81, ProcessorFlags.Sign | ProcessorFlags.Carry | ProcessorFlags.Subtract | ProcessorFlags.HalfCarry)]
		[InlineData(0x80, 0x80, ProcessorFlags.Sign | ProcessorFlags.Carry | ProcessorFlags.Subtract | ProcessorFlags.ParityOverflow)]
		[InlineData(0xFF, 0x01, ProcessorFlags.Carry | ProcessorFlags.Subtract | ProcessorFlags.HalfCarry)]
		[InlineData(0x00, 0x00, ProcessorFlags.Zero | ProcessorFlags.Subtract)]
		public void NEG(byte accValue, byte expectedResult, ProcessorFlags expectedFlags)
		{
			// Arrange
			byte[] code = new byte[] { 0xED, 0x44 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				AF = (ushort)(accValue << 8)
			};
			machine.SetCPUState(initState);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(expectedResult, machine.CPU.A);
			Assert.Equal(expectedFlags, machine.CPU.F);
		}

		[Fact]
		public void RETN()
		{
			// Arrange
			byte[] code = new byte[] { 0xED, 0x45 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				SP = 0x2000,
				IFF2 = true
			};
			machine.SetCPUState(initState);
			machine.Memory.LoadData(new byte[] { 0x55, 0x44 }, 0x2000, false);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(0x4455, machine.CPU.PC);
			Assert.True(machine.CPU.IFF1);
		}

		[Fact]
		public void InterruptMode0()
		{
			// Arrange
			byte[] code = new byte[] { 0xED, 0x46 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				InterruptMode = InterruptMode.Mode1			// Set to Mode1 (to check that IM 0 instruction changes it)
			};
			machine.SetCPUState(initState);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(InterruptMode.Mode0, machine.CPU.InterruptMode);
		}


		[Fact]
		public void LDIAcc()
		{
			// Arrange
			byte[] code = new byte[] { 0xED, 0x47 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				AF = 0x3400
			};
			machine.SetCPUState(initState);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(0x34, machine.CPU.I);
		}

		[Fact]
		public void RETI()
		{
			// Arrange
			byte[] code = new byte[] { 0xED, 0x4D };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				SP = 0x2000
			};
			machine.SetCPUState(initState);
			machine.Memory.LoadData(new byte[] { 0x55, 0x44 }, 0x2000, false);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(0x4455, machine.CPU.PC);
		}

		[Fact]
		public void LDRAcc()
		{
			// Arrange
			byte[] code = new byte[] { 0xED, 0x4F };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				AF = 0x1200
			};
			machine.SetCPUState(initState);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(0x12, machine.CPU.R);
		}

		[Fact]
		public void InterruptMode1()
		{
			// Arrange
			byte[] code = new byte[] { 0xED, 0x56 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				InterruptMode = InterruptMode.Mode2         // Set to Mode2 (to check that IM 1 instruction changes it)
			};
			machine.SetCPUState(initState);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(InterruptMode.Mode1, machine.CPU.InterruptMode);
		}

		[Theory]
		[InlineData(0x00, false, ProcessorFlags.Zero)]
		[InlineData(0x10, true, ProcessorFlags.ParityOverflow)]
		[InlineData(0x80, false, ProcessorFlags.Sign)]
		[InlineData(0x90, true, ProcessorFlags.Sign | ProcessorFlags.ParityOverflow)]
		public void LDAccI(byte value, bool iff2, ProcessorFlags expectedFlags)
		{
			// Arrange
			byte[] code = new byte[] { 0xED, 0x57 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				I = value,
				IFF2 = iff2
			};
			machine.SetCPUState(initState);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(value, machine.CPU.A);
			Assert.Equal(expectedFlags, machine.CPU.F);
		}

		[Fact]
		public void InterruptMode2()
		{
			// Arrange
			byte[] code = new byte[] { 0xED, 0x5E };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				InterruptMode = InterruptMode.Mode1         // Set to Mode1 (to check that IM 2 instruction changes it)
			};
			machine.SetCPUState(initState);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(InterruptMode.Mode2, machine.CPU.InterruptMode);
		}

		[Theory]
		[InlineData(0x00, false, ProcessorFlags.Zero)]
		[InlineData(0x10, true, ProcessorFlags.ParityOverflow)]
		[InlineData(0x80, false, ProcessorFlags.Sign)]
		[InlineData(0x90, true, ProcessorFlags.Sign | ProcessorFlags.ParityOverflow)]
		public void LDAccR(byte value, bool iff2, ProcessorFlags expectedFlags)
		{
			// Arrange
			byte[] code = new byte[] { 0xED, 0x5F };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				R = value,
				IFF2 = iff2
			};
			machine.SetCPUState(initState);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(value, machine.CPU.A);
			Assert.Equal(expectedFlags, machine.CPU.F);
		}

		[Fact]
		public void RRD()
		{
			// Arrange
			byte[] code = new byte[] { 0xED, 0x67 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				HL = 0x2000,
				AF = 0x4500
			};
			machine.SetCPUState(initState);
			machine.Memory.LoadData(new byte[] { 0xA2 }, 0x2000, false);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(0x42, machine.CPU.A);
			Assert.Equal(0x5A, machine.Memory.Data[0x2000]);
			Assert.True(machine.CPU.ParityOverflowFlag);
		}

		[Fact]
		public void RLD()
		{
			// Arrange
			byte[] code = new byte[] { 0xED, 0x6F };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				HL = 0x2000,
				AF = 0x4500
			};
			machine.SetCPUState(initState);
			machine.Memory.LoadData(new byte[] { 0xA2 }, 0x2000, false);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(0x4A, machine.CPU.A);
			Assert.Equal(0x25, machine.Memory.Data[0x2000]);
			Assert.Equal((ProcessorFlags)0, machine.CPU.F);
		}

		[Theory]
		[InlineData(0x0010, ProcessorFlags.ParityOverflow)]
		[InlineData(0x0001, (ProcessorFlags)0)]
		public void LDI(ushort initBC, ProcessorFlags expectedFlags)
		{
			// Arrange
			byte[] code = new byte[] { 0xED, 0xA0 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				BC = initBC,
				HL = 0x2000,
				DE = 0x3000
			};
			machine.SetCPUState(initState);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(initBC - 1, machine.CPU.BC);
			Assert.Equal(0x2001, machine.CPU.HL);
			Assert.Equal(0x3001, machine.CPU.DE);
			Assert.Equal(expectedFlags, machine.CPU.F);
		}

		[Theory]
		[InlineData(0x0010, ProcessorFlags.ParityOverflow)]
		[InlineData(0x0001, (ProcessorFlags)0)]
		public void LDD(ushort initBC, ProcessorFlags expectedFlags)
		{
			// Arrange
			byte[] code = new byte[] { 0xED, 0xA8 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				BC = initBC,
				HL = 0x2000,
				DE = 0x3000
			};
			machine.SetCPUState(initState);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(initBC - 1, machine.CPU.BC);
			Assert.Equal(0x1FFF, machine.CPU.HL);
			Assert.Equal(0x2FFF, machine.CPU.DE);
			Assert.Equal(expectedFlags, machine.CPU.F);
		}

		[Theory]
		[InlineData(0x0010, 0x50, ProcessorFlags.Subtract | ProcessorFlags.ParityOverflow)]
		[InlineData(0x0001, 0x30, ProcessorFlags.Subtract | ProcessorFlags.Sign)]
		[InlineData(0x0010, 0x40, ProcessorFlags.Subtract | ProcessorFlags.Zero | ProcessorFlags.ParityOverflow)]
		public void CPI(ushort initBC, byte initAcc, ProcessorFlags expectedFlags)
		{
			// Arrange
			byte[] code = new byte[] { 0xED, 0xA1 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				BC = initBC,
				HL = 0x2000,
				AF = (ushort)(initAcc << 8)
			};
			machine.SetCPUState(initState);
			machine.Memory.LoadData(new byte[] { 0x40 }, 0x2000, false);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(initBC - 1, machine.CPU.BC);
			Assert.Equal(0x2001, machine.CPU.HL);
			Assert.Equal(expectedFlags, machine.CPU.F);
		}

		[Theory]
		[InlineData(0x0010, 0x50, ProcessorFlags.Subtract | ProcessorFlags.ParityOverflow)]
		[InlineData(0x0001, 0x30, ProcessorFlags.Subtract | ProcessorFlags.Sign)]
		[InlineData(0x0010, 0x40, ProcessorFlags.Subtract | ProcessorFlags.Zero | ProcessorFlags.ParityOverflow)]
		[InlineData(0x0010, 0xFF, ProcessorFlags.Subtract | ProcessorFlags.Sign | ProcessorFlags.ParityOverflow)]
		public void CPD(ushort initBC, byte initAcc, ProcessorFlags expectedFlags)
		{
			// Arrange
			byte[] code = new byte[] { 0xED, 0xA9 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				BC = initBC,
				HL = 0x2000,
				AF = (ushort)(initAcc << 8)
			};
			machine.SetCPUState(initState);
			machine.Memory.LoadData(new byte[] { 0x40 }, 0x2000, false);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(initBC - 1, machine.CPU.BC);
			Assert.Equal(0x1FFF, machine.CPU.HL);
			Assert.Equal(expectedFlags, machine.CPU.F);
		}

		[Theory]
		[InlineData(0x1020, 0xDF, ProcessorFlags.Subtract)]
		[InlineData(0x0130, 0xCF, ProcessorFlags.Subtract | ProcessorFlags.Zero)]
		[InlineData(0x1040, 0xBF, ProcessorFlags.Subtract)]
		public void INI(ushort initBC, byte expectedResult, ProcessorFlags expectedFlags)
		{
			// Arrange
			byte[] code = new byte[] { 0xED, 0xA2 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				BC = initBC,
				HL = 0x2000,
			};
			machine.SetCPUState(initState);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(initBC - 0x0100, machine.CPU.BC);
			Assert.Equal(0x2001, machine.CPU.HL);
			Assert.Equal(expectedResult, machine.Memory.Data[0x2000]);
			Assert.Equal(expectedFlags, machine.CPU.F);
		}

		[Theory]
		[InlineData(0x1050, 0xAF, ProcessorFlags.Subtract)]
		[InlineData(0x0160, 0x9F, ProcessorFlags.Subtract | ProcessorFlags.Zero)]
		[InlineData(0x1070, 0x8F, ProcessorFlags.Subtract)]
		public void IND(ushort initBC, byte expectedResult, ProcessorFlags expectedFlags)
		{
			// Arrange
			byte[] code = new byte[] { 0xED, 0xAA };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				BC = initBC,
				HL = 0x2000,
			};
			machine.SetCPUState(initState);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(initBC - 0x0100, machine.CPU.BC);
			Assert.Equal(0x1FFF, machine.CPU.HL);
			Assert.Equal(expectedResult, machine.Memory.Data[0x2000]);
			Assert.Equal(expectedFlags, machine.CPU.F);
		}

		[Theory]
		[InlineData(0x1020, ProcessorFlags.Subtract)]
		[InlineData(0x0130, ProcessorFlags.Subtract | ProcessorFlags.Zero)]
		[InlineData(0x1040, ProcessorFlags.Subtract)]
		public void OUTI(ushort initBC, ProcessorFlags expectedFlags)
		{
			// Arrange
			byte[] code = new byte[] { 0xED, 0xA3 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				BC = initBC,
				HL = 0x2000,
			};
			machine.SetCPUState(initState);
			machine.Memory.LoadData(new byte[] { 0x33 }, 0x2000, false);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(initBC - 0x0100, machine.CPU.BC);
			Assert.Equal(0x2001, machine.CPU.HL);
			Assert.Equal(0x33, ((DummyPort)machine.Port).DummyData[initBC & 0xFF]);
			Assert.Equal(expectedFlags, machine.CPU.F);
		}

		[Theory]
		[InlineData(0x1070, ProcessorFlags.Subtract)]
		[InlineData(0x0170, ProcessorFlags.Subtract | ProcessorFlags.Zero)]
		[InlineData(0x1090, ProcessorFlags.Subtract)]
		public void OUTD(ushort initBC, ProcessorFlags expectedFlags)
		{
			// Arrange
			byte[] code = new byte[] { 0xED, 0xAB };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				BC = initBC,
				HL = 0x2000,
			};
			machine.SetCPUState(initState);
			machine.Memory.LoadData(new byte[] { 0xAA }, 0x2000, false);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(initBC - 0x0100, machine.CPU.BC);
			Assert.Equal(0x1FFF, machine.CPU.HL);
			Assert.Equal(0xAA, ((DummyPort)machine.Port).DummyData[initBC & 0xFF]);
			Assert.Equal(expectedFlags, machine.CPU.F);
		}

		[Fact]
		public void LDIR()
		{
			// Arrange
			byte[] code = new byte[] { 0xED, 0xB0 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				HL = 0x2000,
				DE = 0x3000,
				BC = 0x0008
			};
			machine.SetCPUState(initState);
			machine.Memory.LoadData(new byte[] { 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88 }, 0x2000, false);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(0x11, machine.Memory.Data[0x3000]);
			Assert.Equal(0x88, machine.Memory.Data[0x3007]);
		}

		[Fact]
		public void LDDR()
		{
			// Arrange
			byte[] code = new byte[] { 0xED, 0xB8 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				HL = 0x2000,
				DE = 0x3000,
				BC = 0x0008
			};
			machine.SetCPUState(initState);
			machine.Memory.LoadData(new byte[] { 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88 }, 0x1FF9, false);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(0x88, machine.Memory.Data[0x3000]);
			Assert.Equal(0x11, machine.Memory.Data[0x2FF9]);
		}

		[Theory]
		[InlineData(0x01, 0x00, ProcessorFlags.Subtract | ProcessorFlags.HalfCarry)]
		[InlineData(0x22, 0x06, ProcessorFlags.Subtract | ProcessorFlags.ParityOverflow | ProcessorFlags.Zero)]
		[InlineData(0x88, 0x00, ProcessorFlags.Subtract | ProcessorFlags.Zero)]
		public void CPIR(byte accValue, ushort expectedBC, ProcessorFlags expectedFlags)
		{
			// Arrange
			byte[] code = new byte[] { 0xED, 0xB1 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				BC = 0x0008,
				HL = 0x2000,
				AF = (ushort)(accValue << 8)
			};
			machine.SetCPUState(initState);
			machine.Memory.LoadData(new byte[] { 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88 }, 0x2000, false);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(expectedBC, machine.CPU.BC);
			Assert.Equal(expectedFlags, machine.CPU.F);
		}

		[Theory]
		[InlineData(0x01, 0x00, ProcessorFlags.Subtract | ProcessorFlags.Sign)]
		[InlineData(0x55, 0x04, ProcessorFlags.Subtract | ProcessorFlags.ParityOverflow | ProcessorFlags.Zero)]
		[InlineData(0x88, 0x07, ProcessorFlags.Subtract | ProcessorFlags.ParityOverflow | ProcessorFlags.Zero)]
		[InlineData(0x11, 0x00, ProcessorFlags.Subtract | ProcessorFlags.Zero)]
		public void CPDR(byte accValue, ushort expectedBC, ProcessorFlags expectedFlags)
		{
			// Arrange
			byte[] code = new byte[] { 0xED, 0xB9 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				BC = 0x0008,
				HL = 0x2000,
				AF = (ushort)(accValue << 8)
			};
			machine.SetCPUState(initState);
			machine.Memory.LoadData(new byte[] { 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88 }, 0x1FF9, false);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(expectedBC, machine.CPU.BC);
			Assert.Equal(expectedFlags, machine.CPU.F);
		}

		[Fact]
		public void INIR()
		{
			// Arrange
			byte[] code = new byte[] { 0xED, 0xB2 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				HL = 0x2000,
				BC = 0x0820
			};
			machine.SetCPUState(initState);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(0xDF, machine.Memory.Data[0x2000]);
			Assert.Equal(0xDF, machine.Memory.Data[0x2007]);
			Assert.True(machine.CPU.SubtractFlag);
			Assert.True(machine.CPU.ZeroFlag);
		}

		[Fact]
		public void INDR()
		{
			// Arrange
			byte[] code = new byte[] { 0xED, 0xBA };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				HL = 0x2000,
				BC = 0x0840
			};
			machine.SetCPUState(initState);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(0xBF, machine.Memory.Data[0x2000]);
			Assert.Equal(0xBF, machine.Memory.Data[0x1FF9]);
			Assert.True(machine.CPU.SubtractFlag);
			Assert.True(machine.CPU.ZeroFlag);
		}

		[Theory]
		[InlineData(0x01, 0x11)]
		[InlineData(0x04, 0x44)]
		[InlineData(0x08, 0x88)]
		public void OTIR(byte initB, ushort expectedValue)
		{
			// Arrange
			byte[] code = new byte[] { 0xED, 0xB3 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				BC = (ushort)((initB << 8) | 0x40),
				HL = 0x2000
			};
			machine.SetCPUState(initState);
			machine.Memory.LoadData(new byte[] { 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88 }, 0x2000, false);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(expectedValue, ((DummyPort)(machine.Port)).DummyData[0x40]);
			Assert.True(machine.CPU.SubtractFlag);
			Assert.True(machine.CPU.ZeroFlag);
		}

		[Theory]
		[InlineData(0x01, 0x88)]
		[InlineData(0x04, 0x55)]
		[InlineData(0x08, 0x11)]
		public void OTDR(byte initB, ushort expectedValue)
		{
			// Arrange
			byte[] code = new byte[] { 0xED, 0xBB };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				BC = (ushort)((initB << 8) | 0x40),
				HL = 0x2000
			};
			machine.SetCPUState(initState);
			machine.Memory.LoadData(new byte[] { 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88 }, 0x1FF9, false);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(expectedValue, ((DummyPort)(machine.Port)).DummyData[0x40]);
			Assert.True(machine.CPU.SubtractFlag);
			Assert.True(machine.CPU.ZeroFlag);
		}


		// ************************
		//
		// DD-prefixed instructions
		//
		// ************************

		[Theory]
		[InlineData(0x09, 0x4000)]
		[InlineData(0x19, 0x5000)]
		[InlineData(0x29, 0x6000)]
		[InlineData(0x39, 0x7000)]
		public void ADDIXRegisterPair(byte instruction, ushort expectedResult)
		{
			// Arrange
			Machine machine = new Machine();
			machine.LoadExecutableData(new byte[] { 0xDD, instruction }, 0x0200);
			CPUState initState = new CPUState
			{
				BC = 0x1000,
				DE = 0x2000,
				SP = 0x4000,
				IX = 0x3000
			};
			machine.SetCPUState(initState);

			// Act
			machine.Execute();

			// Assert
			Assert.Equal(expectedResult, machine.CPU.IX);
		}

		[Fact]
		public void LDIXAddress()
		{
			// Arrange
			byte[] code = new byte[] { 0xDD, 0x21, 0x00, 0x20 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(0x2000, machine.CPU.IX);
		}

		[Fact]
		public void LDAddressIndirectIX()
		{
			// Arrange
			byte[] code = new byte[] { 0xDD, 0x22, 0x00, 0x20 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				IX = 0x3210
			};
			machine.SetCPUState(initState);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(0x10, machine.Memory.Data[0x2000]);
			Assert.Equal(0x32, machine.Memory.Data[0x2001]);
		}

		[Fact]
		public void INCIX()
		{
			// Arrange
			byte[] code = new byte[] { 0xDD, 0x23 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				IX = 0x3210
			};
			machine.SetCPUState(initState);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(0x3211, machine.CPU.IX);
		}

		[Fact]
		public void LDIXAddressIndirect()
		{
			// Arrange
			byte[] code = new byte[] { 0xDD, 0x2A, 0x00, 0x20 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			machine.Memory.LoadData(new byte[] { 0x45, 0x67 }, 0x2000, false);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(0x6745, machine.CPU.IX);
		}

		[Fact]
		public void DECIX()
		{
			// Arrange
			byte[] code = new byte[] { 0xDD, 0x2B };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				IX = 0x3210
			};
			machine.SetCPUState(initState);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(0x320F, machine.CPU.IX);
		}

		[Fact]
		public void POPIX()
		{
			// Arrange
			byte[] code = new byte[] { 0xDD, 0xE1 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				SP = 0x2000
			};
			machine.SetCPUState(initState);
			machine.Stack.Push(0x1234);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(0x1234, machine.CPU.IX);
		}

		[Fact]
		public void EXSPIX()
		{
			// Arrange
			byte[] code = new byte[] { 0xDD, 0xE3 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				IX = 0x3456,
				SP = 0x2000
			};
			machine.SetCPUState(initState);
			machine.Stack.Push(0x1234);

			// Act
			machine.Execute();

			// Assert
			Assert.Equal(0x1234, machine.CPU.IX);
			Assert.Equal(0x1FFE, machine.CPU.SP);
			Assert.Equal(0x34, machine.Memory.Data[0x1FFF]);
			Assert.Equal(0x56, machine.Memory.Data[0x1FFE]);
		}

		[Fact]
		public void PUSHIX()
		{
			// Arrange
			byte[] code = new byte[] { 0xDD, 0xE5 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				IX = 0x3456,
				SP = 0x2000
			};
			machine.SetCPUState(initState);

			// Act
			machine.Execute();

			// Assert
			Assert.Equal(0x1FFE, machine.CPU.SP);
			Assert.Equal(0x34, machine.Memory.Data[0x1FFF]);
			Assert.Equal(0x56, machine.Memory.Data[0x1FFE]);
		}

		[Fact]
		public void JPIX()
		{
			// Arrange
			byte[] code = new byte[] { 0xDD, 0xE9 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				IX = 0x3456,
			};
			machine.SetCPUState(initState);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(0x3456, machine.CPU.PC);
		}

		[Fact]
		public void LDSPIX()
		{
			// Arrange
			byte[] code = new byte[] { 0xDD, 0xF9 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				IX = 0x3456,
			};
			machine.SetCPUState(initState);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(0x3456, machine.CPU.SP);
		}


		// ************************
		//
		// FD-prefixed instructions
		//
		// ************************

		[Theory]
		[InlineData(0x09, 0x4000)]
		[InlineData(0x19, 0x5000)]
		[InlineData(0x29, 0x6000)]
		[InlineData(0x39, 0x7000)]
		public void ADDIYRegisterPair(byte instruction, ushort expectedResult)
		{
			// Arrange
			Machine machine = new Machine();
			machine.LoadExecutableData(new byte[] { 0xFD, instruction }, 0x0200);
			CPUState initState = new CPUState
			{
				BC = 0x1000,
				DE = 0x2000,
				SP = 0x4000,
				IY = 0x3000
			};
			machine.SetCPUState(initState);

			// Act
			machine.Execute();

			// Assert
			Assert.Equal(expectedResult, machine.CPU.IY);
		}

		[Fact]
		public void LDIYAddress()
		{
			// Arrange
			byte[] code = new byte[] { 0xFD, 0x21, 0x00, 0x20 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(0x2000, machine.CPU.IY);
		}

		[Fact]
		public void LDAddressIndirectIY()
		{
			// Arrange
			byte[] code = new byte[] { 0xFD, 0x22, 0x00, 0x20 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				IY = 0x3210
			};
			machine.SetCPUState(initState);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(0x10, machine.Memory.Data[0x2000]);
			Assert.Equal(0x32, machine.Memory.Data[0x2001]);
		}

		[Fact]
		public void INCIY()
		{
			// Arrange
			byte[] code = new byte[] { 0xFD, 0x23 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				IY = 0x3210
			};
			machine.SetCPUState(initState);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(0x3211, machine.CPU.IY);
		}

		[Fact]
		public void LDIYAddressIndirect()
		{
			// Arrange
			byte[] code = new byte[] { 0xFD, 0x2A, 0x00, 0x20 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			machine.Memory.LoadData(new byte[] { 0x45, 0x67 }, 0x2000, false);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(0x6745, machine.CPU.IY);
		}

		[Fact]
		public void DECIY()
		{
			// Arrange
			byte[] code = new byte[] { 0xFD, 0x2B };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				IY = 0x3210
			};
			machine.SetCPUState(initState);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(0x320F, machine.CPU.IY);
		}

		[Fact]
		public void POPIY()
		{
			// Arrange
			byte[] code = new byte[] { 0xFD, 0xE1 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				SP = 0x2000
			};
			machine.SetCPUState(initState);
			machine.Stack.Push(0x1234);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(0x1234, machine.CPU.IY);
		}

		[Fact]
		public void EXSPIY()
		{
			// Arrange
			byte[] code = new byte[] { 0xFD, 0xE3 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				IY = 0x3456,
				SP = 0x2000
			};
			machine.SetCPUState(initState);
			machine.Stack.Push(0x1234);

			// Act
			machine.Execute();

			// Assert
			Assert.Equal(0x1234, machine.CPU.IY);
			Assert.Equal(0x1FFE, machine.CPU.SP);
			Assert.Equal(0x34, machine.Memory.Data[0x1FFF]);
			Assert.Equal(0x56, machine.Memory.Data[0x1FFE]);
		}

		[Fact]
		public void PUSHIY()
		{
			// Arrange
			byte[] code = new byte[] { 0xFD, 0xE5 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				IY = 0x3456,
				SP = 0x2000
			};
			machine.SetCPUState(initState);

			// Act
			machine.Execute();

			// Assert
			Assert.Equal(0x1FFE, machine.CPU.SP);
			Assert.Equal(0x34, machine.Memory.Data[0x1FFF]);
			Assert.Equal(0x56, machine.Memory.Data[0x1FFE]);
		}

		[Fact]
		public void JPIY()
		{
			// Arrange
			byte[] code = new byte[] { 0xFD, 0xE9 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				IY = 0x3456,
			};
			machine.SetCPUState(initState);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(0x3456, machine.CPU.PC);
		}

		[Fact]
		public void LDSPIY()
		{
			// Arrange
			byte[] code = new byte[] { 0xFD, 0xF9 };
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				IY = 0x3456,
			};
			machine.SetCPUState(initState);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(0x3456, machine.CPU.SP);
		}

		[Fact]
		public void LDIYIndirectImm()
		{
			// Arrange
			byte[] code = new byte[] { 0xFD, 0x36, 0x05, 0x7F };            // 0x05 is the displacement - i.e. (IY + 0x05)
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				IY = 0x2000
			};
			machine.SetCPUState(initState);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(0x7F, machine.Memory.Data[0x2005]);      // 0x2005 is (IY + 0x05)
		}



		// **************************************************************
		//
		// DDCB-prefixed instructions - i.e. (IX+d) instructions.
		//
		// NOTE: these instructions use the (HL) equivalent handlers
		// (i.e. the CB-prefixed handlers) so only a couple are actually
		// tested here because the functionality of the underlying
		// CB-prefixed handlers are tested elsewhere.
		//
		// **************************************************************

		[Theory]
		[InlineData(0x04, 0x08, (ProcessorFlags)0)]
		[InlineData(0x80, 0x01, ProcessorFlags.Carry)]
		[InlineData(0xAA, 0x55, ProcessorFlags.Carry | ProcessorFlags.ParityOverflow)]
		[InlineData(0x40, 0x80, ProcessorFlags.Sign)]
		[InlineData(0x00, 0x00, ProcessorFlags.Zero | ProcessorFlags.ParityOverflow)]
		public void RLCIXIndirect(byte initValue, byte expectedResult, ProcessorFlags expectedFlags)
		{
			// Arrange
			byte[] code = new byte[] { 0xDD, 0xCB, 0x04, 0x06 };			// 0x04 is the displacement - i.e. (IX + 0x04)
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				IX = 0x2000
			};
			machine.SetCPUState(initState);
			machine.Memory.LoadData(new byte[] { 0x00, 0x01, 0x02, 0x03, initValue, 0x04, 0x05, 0x06 }, 0x2000, false);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(expectedResult, machine.Memory.Data[0x2004]);      // 0x2004 is (IX + 0x04)
			Assert.Equal(expectedFlags, machine.CPU.F);
		}

		[Theory]
		[InlineData(0xC6, 0x5B)]          // SET 0,(IX+d)
		[InlineData(0xDE, 0x5A)]          // SET 3,(IX+d)
		[InlineData(0xFE, 0xDA)]          // SET 7,(IX+d)
		public void SETIXIndirect(byte instruction, ushort expectedResult)
		{
			// Arrange
			byte[] code = new byte[] { 0xDD, 0xCB, 0x03, instruction };         // 0x03 is the displacement - i.e. (IX + 0x03)
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				IX = 0x2000
			};
			machine.SetCPUState(initState);
			machine.Memory.LoadData(new byte[] { 0x02, 0x04, 0x05, 0x5A, 0x05, 0x07, 0x08 }, 0x2000, false);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(expectedResult, machine.Memory.Data[0x2003]);		// 0x2003 is (IX + 0x03)
		}


		// **************************************************************
		//
		// FDCB-prefixed instructions - i.e. (IX+d) instructions.
		//
		// NOTE: these instructions use the (HL) equivalent handlers
		// (i.e. the CB-prefixed handlers) so only a couple are actually
		// tested here because the functionality of the underlying
		// CB-prefixed handlers are tested elsewhere.
		//
		// **************************************************************

		[Theory]
		[InlineData(0x04, 0x02, (ProcessorFlags)0)]
		[InlineData(0x81, 0x40, ProcessorFlags.Carry)]
		[InlineData(0x01, 0x00, ProcessorFlags.Carry | ProcessorFlags.Zero | ProcessorFlags.ParityOverflow)]
		[InlineData(0x55, 0x2A, ProcessorFlags.Carry)]
		[InlineData(0xAA, 0x55, ProcessorFlags.ParityOverflow)]
		public void SRLIYIndirect(byte initValue, byte expectedResult, ProcessorFlags expectedFlags)
		{
			// Arrange
			byte[] code = new byte[] { 0xFD, 0xCB, 0x02, 0x3E };         // 0x02 is the displacement - i.e. (IY + 0x02)
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				IY = 0x2000
			};
			machine.SetCPUState(initState);
			machine.Memory.LoadData(new byte[] { 0x02, 0x03, initValue, 0x04, 0x05, 0x07, 0x08 }, 0x2000, false);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(expectedResult, machine.Memory.Data[0x2002]);      // 0x2002 is (IY + 0x02)
			Assert.Equal(expectedFlags, machine.CPU.F);
		}

		[Theory]
		[InlineData(0x46, ProcessorFlags.HalfCarry | ProcessorFlags.Zero)]      // BIT 0,(IY+d)
		[InlineData(0x5E, ProcessorFlags.HalfCarry)]                            // BIT 3,(IY+d)
		[InlineData(0x76, ProcessorFlags.HalfCarry)]                            // BIT 6,(IY+d)
		[InlineData(0x7E, ProcessorFlags.HalfCarry | ProcessorFlags.Zero)]      // BIT 7,(IY+d)
		public void BITIYIndirect(byte instruction, ProcessorFlags expectedFlags)
		{
			// Arrange
			byte[] code = new byte[] { 0xFD, 0xCB, 0x07, instruction };         // 0x07 is the displacement - i.e. (IY + 0x07)
			Machine machine = new Machine();
			machine.LoadExecutableData(code, 0x0200);
			CPUState initState = new CPUState
			{
				IY = 0x2000
			};
			machine.SetCPUState(initState);
			machine.Memory.LoadData(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x5A, 0x08, 0x09 }, 0x2000, false);

			// Act
			machine.ExecuteInstruction();

			// Assert
			Assert.Equal(expectedFlags, machine.CPU.F);
		}


	}
}
