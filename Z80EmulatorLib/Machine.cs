using System;

namespace PendleCodeMonkey.Z80EmulatorLib
{
	/// <summary>
	/// Implementation of the <see cref="Machine"/> class.
	/// </summary>
	public class Machine
	{
		private ushort _loadedAddress;
		private ushort _dataLength;

		/// <summary>
		/// Initializes a new instance of the <see cref="Machine"/> class.
		/// </summary>
		/// <param name="port">The <see cref="IPort"/> instance to be used for IN/OUT instructions.</param>
		public Machine(IPort port = null)
		{
			CPU = new CPU();
			Memory = new Memory();
			Stack = new Stack(CPU, Memory);
			ExecutionHandler = new OpcodeExecutionHandler(this);
			Decoder = new InstructionDecoder(this);
			Port = port ?? new DummyPort();
		}

		/// <summary>
		/// Gets the <see cref="CPU"/> instance used by this machine.
		/// </summary>
		internal CPU CPU { get; private set; }

		/// <summary>
		/// Gets the <see cref="Memory"/> instance used by this machine.
		/// </summary>
		internal Memory Memory { get; private set; }

		/// <summary>
		/// Gets the <see cref="Stack"/> instance used by this machine.
		/// </summary>
		internal Stack Stack { get; private set; }

		/// <summary>
		/// Gets the <see cref="InstructionDecoder"/> instance used by this machine.
		/// </summary>
		/// <remarks>
		/// This instance decodes byte data into Z80 instructions.
		/// </remarks>
		internal InstructionDecoder Decoder { get; private set; }

		/// <summary>
		/// Gets the <see cref="OpcodeExecutionHandler"/> instance used by this machine.
		/// </summary>
		/// <remarks>
		/// This instance performs the execution of the Z80 instructions.
		/// </remarks>
		internal OpcodeExecutionHandler ExecutionHandler { get; private set; }

		/// <summary>
		/// Gets the <see cref="IPort"/> instance used by this machine.
		/// </summary>
		/// <remarks>
		/// This instance is used by the Z80 IN/OUT instructions.
		/// </remarks>
		internal IPort Port { get; private set; }


		/// <summary>
		/// Gets a value indicating if the machine has reached the end of the loaded executable data.
		/// </summary>
		internal bool IsEndOfData => CPU.PC >= _loadedAddress + _dataLength;

		/// <summary>
		/// Gets a value indicating if the execution of code has been terminated.
		/// </summary>
		/// <remarks>
		/// This can occur when a RFET instruction has been executed that was not within a subroutine invoked
		/// via the CALL instruction (i.e. a RET instruction intended to mark the end of execution).
		/// </remarks>
		internal bool IsEndOfExecution { get; set; }

		/// <summary>
		/// Reset the machine to its default state.
		/// </summary>
		public void Reset()
		{
			Memory.Clear();
			CPU.Reset();
			IsEndOfExecution = false;
			_loadedAddress = 0;
			_dataLength = 0;
		}

		/// <summary>
		/// Dumps the current state of the machine in a string format.
		/// </summary>
		/// <returns>A string containing details of the current state of the machine.</returns>
		public string Dump()
		{
			string dump = "";

			dump += $"A: 0x{CPU.A:X2} ({CPU.A})";
			dump += Environment.NewLine;
			dump += "Flags: " + CPU.F.ToString();
			dump += Environment.NewLine;
			dump += $"BC: 0x{CPU.BC:X4} ({CPU.BC})";
			dump += Environment.NewLine;
			dump += $"DE: 0x{CPU.DE:X4} ({CPU.DE})";
			dump += Environment.NewLine;
			dump += $"HL: 0x{CPU.HL:X4} ({CPU.HL})";
			dump += Environment.NewLine;
			dump += $"IX: 0x{CPU.IX:X4} ({CPU.IX})";
			dump += Environment.NewLine;
			dump += $"IY: 0x{CPU.IY:X4} ({CPU.IY})";
			dump += Environment.NewLine;
			dump += $"SP: 0x{CPU.SP:X4} ({CPU.SP})";
			dump += Environment.NewLine;
			dump += $"PC: 0x{CPU.PC:X4} ({CPU.PC})";
			dump += Environment.NewLine;
			dump += $"AF': 0x{CPU.AF_Shadow:X4} ({CPU.AF_Shadow})";
			dump += Environment.NewLine;
			dump += $"BC': 0x{CPU.BC_Shadow:X4} ({CPU.BC_Shadow})";
			dump += Environment.NewLine;
			dump += $"DE': 0x{CPU.DE_Shadow:X4} ({CPU.DE_Shadow})";
			dump += Environment.NewLine;
			dump += $"HL': 0x{CPU.HL_Shadow:X4} ({CPU.HL_Shadow})";
			dump += Environment.NewLine;

			return dump;
		}

		/// <summary>
		/// Get a <see cref="CPUState"/> object containing the current CPU state settings (i.e. register
		/// values, Program Counter, Stack Pointer, etc.)
		/// </summary>
		/// <returns>A <see cref="CPUState"/> object containing the current CPU state settings.</returns>
		public CPUState GetCPUState()
		{
			CPUState state = new CPUState();
			state.TransferStateFromCPU(CPU);
			return state;
		}

		/// <summary>
		/// Set the state of settings in the CPU according to the values in the supplied <see cref="CPUState"/> object.
		/// </summary>
		/// <remarks>
		/// Only non-null values in the supplied <see cref="CPUState"/> instance will be transferred to the CPU, all other
		/// CPU settings will be unaffected.
		/// </remarks>
		/// <param name="state">A <see cref="CPUState"/> object containing the new CPU state settings.</param>
		public void SetCPUState(CPUState state)
		{
			state.TransferStateToCPU(CPU);
		}

		/// <summary>
		/// Load executable data into memory at the specified address.
		/// </summary>
		/// <remarks>
		/// Loading executable data also sets the Program Counter to the address of the loaded data.
		/// </remarks>
		/// <param name="data">The executable data to be loaded.</param>
		/// <param name="loadAddress">The address at which the executable data should be loaded.</param>
		/// <param name="clearBeforeLoad"><c>true</c> if all memory should be cleared prior to loading the data, otherwise <c>false</c>.</param>
		/// <returns><c>true</c> if the executable data was successfully loaded, otherwise <c>false</c>.</returns>
		public bool LoadExecutableData(byte[] data, ushort loadAddress, bool clearBeforeLoad = true)
		{
			if (Memory.LoadData(data, loadAddress, clearBeforeLoad))
			{
				CPU.PC = loadAddress;
				_loadedAddress = loadAddress;
				_dataLength = (ushort)data.Length;
				return true;
			}
			return false;
		}

		/// <summary>
		/// Load non-executable data into memory at the specified address.
		/// </summary>
		/// <param name="data">The data to be loaded.</param>
		/// <param name="loadAddress">The address at which the data should be loaded.</param>
		/// <param name="clearBeforeLoad"><c>true</c> if all memory should be cleared prior to loading the data, otherwise <c>false</c>.</param>
		/// <returns><c>true</c> if the data was successfully loaded, otherwise <c>false</c>.</returns>
		public bool LoadData(byte[] data, ushort loadAddress, bool clearBeforeLoad = true)
		{
			return Memory.LoadData(data, loadAddress, clearBeforeLoad);
		}

		/// <summary>
		/// Return the specified block of memory.
		/// </summary>
		/// <param name="address">Start address of the requested block of memory.</param>
		/// <param name="length">Length (in bytes) of the block of memory to be retrieved.</param>
		/// <returns>Read-only copy of the requested memory.</returns>
		public ReadOnlySpan<byte> DumpMemory(ushort address, ushort length)
		{
			return Memory.DumpMemory(address, length);
		}

		/// <summary>
		/// Start executing instructions from the current Program Counter address.
		/// </summary>
		/// <remarks>
		/// This method keeps executing instructions until the program terminates and is therefore the
		/// main entry point for executing Z80 code.
		/// </remarks>
		public void Execute()
		{
			while (!IsEndOfData && !IsEndOfExecution)
			{
				ExecuteInstruction();
			}
		}

		/// <summary>
		/// Execute a single instruction located at the current Program Counter address.
		/// </summary>
		public void ExecuteInstruction()
		{
			var instruction = Decoder.FetchInstruction();
			ExecutionHandler.Execute(instruction);
		}
	}
}
