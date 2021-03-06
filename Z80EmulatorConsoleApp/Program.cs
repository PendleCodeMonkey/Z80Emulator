using PendleCodeMonkey.Z80EmulatorLib;
using PendleCodeMonkey.Z80EmulatorLib.Assembler;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Z80EmulatorConsoleApp
{
	class Program
	{
		static void Main(string[] _)
		{
			MemCpy();

			TestDisassembler();

			DivisionTest();

			AssemblerTest();

			AssemblerTestExternalFile();

			// Checks that the binary data generated by the assembler in AssemblerTestExternalFile() is identical
			// to that in a pre-built 48k.rom file.
			TestBinaryData();
		}

		static void MemCpy()
		{
			Console.WriteLine("MemCpy example:");
			Console.WriteLine("---------------");

			Machine machine = new Machine();

			CPUState state = new CPUState
			{
				DE = 0x2000,			// Source
				HL = 0x3000,			// Destination
				BC = 0x0010,			// Num bytes
				SP = 0x4000
			};

			// Code to copy BC bytes from address in DE to address in HL (i.e. copy 0x0010 bytes fromm 0x2000 to 0x3000)
			byte[] code = new byte[] { 0x78, 0xB1, 0xC8, 0x1A, 0x77, 0x13, 0x23, 0x0B, 0xC3, 0x00, 0x10 };
			machine.LoadExecutableData(code, 0x1000);
			machine.SetCPUState(state);

			// Load some data into 0x2000
			byte[] data = new byte[] { 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0xAA };
			machine.LoadData(data, 0x2000, false);

			machine.Execute();

			// Dump out the contents of memory address 0x3000 - 0x300A (to show the code has worked)
			Console.WriteLine();
			Console.WriteLine("Memory dump:");
			var mem = machine.DumpMemory(0x3000, 0x000A);
			for (int n = 0; n < 0x0A; n++)
			{
				Console.Write($"{mem[n]}  ");
			}

			Console.WriteLine();

			Console.WriteLine();
			Console.WriteLine("CPU state dump:");
			string dump = machine.Dump();
			Console.WriteLine(dump);

		}

		static void TestDisassembler()
		{
			Console.WriteLine();
			Console.WriteLine("Disassembler example:");
			Console.WriteLine("---------------------");
			Console.WriteLine();

			Machine machine = new Machine();

			byte[] code = new byte[] { 0x37, 0x3F, 0xDD, 0x7E, 0x00, 0xFD, 0x8E, 0x00, 0x77, 0xDD, 0x2B, 0xFD, 0x2B, 0x2B, 0x10, 0xF2, 0xC9,
										0x11, 0x22, 0x22, 0x33, 0x55, 0x66, 0x77, 0x88, 0x99, 0xAA,
										0x21, 0x00, 0x00, 0x3E, 0x10, 0xCB, 0x21, 0xCB, 0x10, 0xED, 0x6A, 0xED,
										0x52, 0x38, 0x03, 0x0C, 0x18, 0x01, 0x19, 0x3D, 0x20, 0xEF, 0xC9};
			machine.LoadExecutableData(code, 0x1000);

			Disassembler disassembler = new Disassembler(machine, startAddress: 0x1000, length: (ushort)code.Length);
			disassembler.AddNonExecutableSection(0x1011, 0x000A);
			var dis = disassembler.Disassemble();

			foreach (var (Address, Disassembly) in dis)
			{
				Console.WriteLine($"0x{Address:X4} - {Disassembly}");
			}

		}


		static void DivisionTest()
		{
			Console.WriteLine();
			Console.WriteLine("16-bit division example:");
			Console.WriteLine("------------------------");
			Machine machine = new Machine();

			CPUState state = new CPUState
			{
				BC = 0x3264,
				DE = 0x001B,
				SP = 0x4000
			};

			// Code that divides BC by DE; giving result in BC and remainder in HL
			byte[] code = new byte[] { 0x21, 0x00, 0x00, 0x3E, 0x10, 0xCB, 0x21, 0xCB, 0x10, 0xED, 0x6A, 0xED,
										0x52, 0x38, 0x03, 0x0C, 0x18, 0x01, 0x19, 0x3D, 0x20, 0xEF, 0xC9 };
			machine.LoadExecutableData(code, 0x0000);
			machine.SetCPUState(state);

			machine.Execute();

			Console.WriteLine();
			Console.WriteLine("CPU state dump:");

			string dump = machine.Dump();
			Console.WriteLine(dump);

		}

		static void AssemblerTest()
		{
			Console.WriteLine();
			Console.WriteLine("Testing Assembler using hard-coded source file:");
			Console.WriteLine("-----------------------------------------------");
			Console.WriteLine();

			var code = GetZ80DivisionSourceCode();
			if (code != null && code.Count > 0)
			{
				Console.WriteLine("ASSEMBLING CODE.  Please wait...");
				Console.WriteLine();

				Assembler asm = new Assembler();
				var (success, binData) = asm.Assemble(code);

				if (!success)
				{
					foreach (var (lineNumber, Error, AdditionalInfo) in asm.AsmErrors)
					{
						Console.WriteLine($"{Error} ({AdditionalInfo}) on line {lineNumber}");
					}
				}
				else
				{
					// Fire up an instance of the emulator and load the assembled binary data into it.
					Machine machine = new Machine();
					machine.LoadExecutableData(binData.ToArray(), 0x8000);

					// Execute the assembled binary data.
					machine.Execute();

					// Get the last 8 bytes of the binary data (which are DS segments that contain the results of the operation)
					var memory = machine.DumpMemory((ushort)(0x8000 + binData.Count - 8), 8);

					short result1 = (short)((memory[1] << 8) + memory[0]);
					short remainder1 = (short)((memory[3] << 8) + memory[2]);

					ushort result2 = (ushort)((memory[5] << 8) + memory[4]);
					ushort remainder2 = (ushort)((memory[7] << 8) + memory[6]);

					Console.WriteLine($"Signed division (-2047 / 127). Result: {result1}, Remainder: {remainder1}");
					Console.WriteLine($"Unsigned division (54321 / 135). Result: {result2}, Remainder: {remainder2}");
				}
			}
		}

		static List<string> GetZ80DivisionSourceCode()
		{
			List<string> source = new List<string>
			{
				"ORG 8000h",
				"LD HL,-2047",
				"LD DE,127",
				"CALL SDIV16",
				"LD(SIGNEDRESULT),HL",
				"LD(SIGNEDREMAINDER),DE",
				"LD HL,54321",
				"LD DE,135",
				"CALL UDIV16",
				"LD(UNSIGNEDRESULT),HL",
				"LD(UNSIGNEDREMAINDER),DE",
				"RET",
				"SDIV16:",
				"LD A, H",
				"LD(SREM),A",
				"XOR D",
				"LD(SQUOT),A",
				"LD A,D",
				"OR A",
				"JP P,CHKDE",
				"SUB A",
				"SUB E",
				"LD E,A",
				"SBC A,A",
				"SUB D",
				"LD D,A",
				"CHKDE:",
				"LD A, H",
				"OR A",
				"JP P, DODIV",
				"SUB A",
				"SUB L",
				"LD L, A",
				"SBC A, A",
				"SUB H",
				"LD H, A",
				"DODIV:",
				"CALL UDIV16",
				"RET C",
				"LD A,(SQUOT)",
				"OR A",
				"JP P, DOREM",
				"SUB A",
				"SUB L",
				"LD L, A",
				"SBC A, A",
				"SUB H",
				"LD H, A",
				"DOREM:",
				"LD A,(SREM)",
				"OR A",
				"RET P",
				"SUB A",
				"SUB E",
				"LD E, A",
				"SBC A, A",
				"SUB D",
				"LD D, A",
				"RET",
				"UDIV16:",
				"LD A, E",
				"OR D",
				"JR NZ, DIVIDE",
				"LD HL,0",
				"LD D, H",
				"LD E, L",
				"SCF",
				"RET",
				"DIVIDE:",
				"LD C, L",
				"LD A, H",
				"LD HL,0",
				"LD B,16",
				"OR A",
				"DVLOOP:",
				"RL C",
				"RLA",
				"RL L",
				"RL H",
				"PUSH HL",
				"SBC HL,DE",
				"CCF",
				"JR C, DROP",
				"EX(SP),HL",
				"DROP:",
				"INC SP",
				"INC SP",
				"DJNZ DVLOOP",
				"EX DE, HL",
				"RL C",
				"LD L, C",
				"RLA",
				"LD H,A",
				"OR A",
				"RET",
				"SQUOT: DS 1",
				"SREM: DS 1",
				"SIGNEDRESULT: DS 2",
				"SIGNEDREMAINDER: DS 2",
				"UNSIGNEDRESULT: DS 2",
				"UNSIGNEDREMAINDER: DS 2"
			};

			return source;
		}

		static void AssemblerTestExternalFile()
		{
			Console.WriteLine();
			Console.WriteLine("Testing Assembler using external source file:");
			Console.WriteLine("---------------------------------------------");
			Console.WriteLine();

			string filename = "code.asm";
			var code = ReadCode(filename);
			if (code != null && code.Count > 0)
			{
				Console.WriteLine("ASSEMBLING CODE.  Please wait...");
				Console.WriteLine();

				Assembler asm = new Assembler();
				var (success, binData) = asm.Assemble(code);

				if (!success)
				{
					foreach (var (lineNumber, Error, AdditionalInfo) in asm.AsmErrors)
					{
						Console.WriteLine($"{Error} ({AdditionalInfo}) on line {lineNumber}");
					}
				}
				else
				{
					// Write the generated binary data to a file.
					byte[] binDataArray = binData.ToArray();
					string path = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
					path = Path.Combine(path, "AsmGenerated.bin");
					File.WriteAllBytes(path, binDataArray);

					// Fire up an instance of the emulator and get it to produce a disassembly of the generated binary code.
					Machine machine = new Machine();
					machine.LoadExecutableData(binData.ToArray(), 0);

					Disassembler disassembler = new Disassembler(machine, startAddress: 0, length: (ushort)binData.Count);

					// Mark the data segments that were created during assembly as non-executable blocks.
					foreach (var (startAddress, size) in asm.DataSegments)
					{
						disassembler.AddNonExecutableSection(startAddress, size);
					}

					// Disassemble the generated binary data.
					var dis = disassembler.Disassemble();

					// Write disassembly output to a file.
					string path2 = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
					path2 = Path.Combine(path2, "Disassembled.asm");
					try
					{
						using StreamWriter writer = new StreamWriter(path2);
						foreach (var (Address, Disassembly) in dis)
						{
							writer.WriteLine($"{Disassembly}");
						}
					}
					catch (Exception)
					{
						Console.WriteLine("Error occurred attempting to write file: Disassembled.asm");
					}
				}
			}
		}

		private static List<string> ReadCode(string filename)
		{
			List<string> lines = new List<string>();

			try
			{
				string path = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
				path = Path.Combine(path, filename);
				using StreamReader sr = new StreamReader(path);
				while (sr.Peek() >= 0)
				{
					string line = sr.ReadLine();
					lines.Add(line);
				}
			}
			catch (Exception)
			{
				Console.WriteLine("Error occurred attempting to read file: " + filename);
			}

			return lines;
		}

		private static void TestBinaryData()
		{
			Console.WriteLine();
			Console.WriteLine("Checking Assembler-generated binary matches ROM file:");
			Console.WriteLine("-----------------------------------------------------");
			Console.WriteLine();

			byte[] binDataArray1;
			try
			{
				string path = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
				path = Path.Combine(path, "AsmGenerated.bin");
				binDataArray1 = File.ReadAllBytes(path);
			}
			catch (Exception)
			{
				Console.WriteLine("Error occurred attempting to read file: AsmGenerated.bin");
				return;
			}

			byte[] binDataArray2;
			try
			{
				string path2 = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
				path2 = Path.Combine(path2, "48k.rom");
				binDataArray2 = File.ReadAllBytes(path2);
			}
			catch (Exception)
			{
				Console.WriteLine("Error occurred attempting to read file: 48k.rom");
				return;
			}

			int diffCount = 0;
			if (binDataArray1.Length != binDataArray2.Length)
			{
				diffCount = 1;
			}
			else
			{
				for (int i = 0; i < binDataArray1.Length; i++)
				{
					if (binDataArray1[i] != binDataArray2[i])
					{
						diffCount++;
					}
				}
			}

			Console.WriteLine("Binary files " + (diffCount == 0 ? "are identical." : "are NOT identical."));
		}
	}
}
