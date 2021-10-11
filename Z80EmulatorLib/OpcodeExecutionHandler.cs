using PendleCodeMonkey.Z80EmulatorLib.Enumerations;
using System;
using System.Collections.Generic;

namespace PendleCodeMonkey.Z80EmulatorLib
{
	/// <summary>
	/// Implementation of the <see cref="OpcodeExecutionHandler"/> class.
	/// </summary>
	class OpcodeExecutionHandler
	{
		private Dictionary<OpHandlerID, Action<Instruction>> _handlers = new Dictionary<OpHandlerID, Action<Instruction>>();

		private readonly byte[] _bitMap = new byte[] { 0x01, 0x02, 0x04, 0x08, 0x10, 0x20, 0x40, 0x80 };

		private int _numberOfCalls = 0;

		/// <summary>
		/// Initializes a new instance of the <see cref="OpcodeExecutionHandler"/> class.
		/// </summary>
		/// <param name="machine">The <see cref="Machine"/> instance for which this object is handling the execution of instructions.</param>
		public OpcodeExecutionHandler(Machine machine)
		{
			Machine = machine ?? throw new ArgumentNullException(nameof(machine));

			InitOpcodeHandlers();
		}

		/// <summary>
		/// Gets or sets the <see cref="Machine"/> instance for which this <see cref="OpcodeExecutionHandler"/> instance
		/// is handling the execution of instructions
		/// </summary>
		private Machine Machine { get; set; }

		/// <summary>
		/// Initialize the dictionary of Opcode handlers.
		/// </summary>
		/// <remarks>
		/// Maps an enumerated operation handler ID to an Action that performs the operation.
		/// </remarks>
		private void InitOpcodeHandlers()
		{
			// Unprefixed opcode handlers
			_handlers.Add(OpHandlerID.NOP, NOP);
			_handlers.Add(OpHandlerID.LDRegister, LDRegister);
			_handlers.Add(OpHandlerID.LDRegisterHL, LDRegisterHL);
			_handlers.Add(OpHandlerID.LDHLRegister, LDHLRegister);
			_handlers.Add(OpHandlerID.ADDAccRegister, ADDAccRegister);
			_handlers.Add(OpHandlerID.ADDAccHL, ADDAccHL);
			_handlers.Add(OpHandlerID.ADCAccRegister, ADCAccRegister);
			_handlers.Add(OpHandlerID.ADCAccHL, ADCAccHL);
			_handlers.Add(OpHandlerID.SUBAccRegister, SUBAccRegister);
			_handlers.Add(OpHandlerID.SUBAccHL, SUBAccHL);
			_handlers.Add(OpHandlerID.SBCAccRegister, SBCAccRegister);
			_handlers.Add(OpHandlerID.SBCAccHL, SBCAccHL);
			_handlers.Add(OpHandlerID.ANDRegister, ANDRegister);
			_handlers.Add(OpHandlerID.ANDHL, ANDHL);
			_handlers.Add(OpHandlerID.XORRegister, XORRegister);
			_handlers.Add(OpHandlerID.XORHL, XORHL);
			_handlers.Add(OpHandlerID.ORRegister, ORRegister);
			_handlers.Add(OpHandlerID.ORHL, ORHL);
			_handlers.Add(OpHandlerID.CPRegister, CPRegister);
			_handlers.Add(OpHandlerID.CPHL, CPHL);
			_handlers.Add(OpHandlerID.INCRegister, INCRegister);
			_handlers.Add(OpHandlerID.DECRegister, DECRegister);
			_handlers.Add(OpHandlerID.INCRegisterPair, INCRegisterPair);
			_handlers.Add(OpHandlerID.DECRegisterPair, DECRegisterPair);
			_handlers.Add(OpHandlerID.HALT, HALT);
			_handlers.Add(OpHandlerID.RETCondition, RETCondition);
			_handlers.Add(OpHandlerID.JPCondition, JPCondition);
			_handlers.Add(OpHandlerID.CALLCondition, CALLCondition);
			_handlers.Add(OpHandlerID.POPRegisterPair, POPRegisterPair);
			_handlers.Add(OpHandlerID.JP, JP);
			_handlers.Add(OpHandlerID.PUSHRegisterPair, PUSHRegisterPair);
			_handlers.Add(OpHandlerID.RET, RET);
			_handlers.Add(OpHandlerID.CALL, CALL);
			_handlers.Add(OpHandlerID.EXX, EXX);
			_handlers.Add(OpHandlerID.EXSPHL, EXSPHL);
			_handlers.Add(OpHandlerID.JPHL, JPHL);
			_handlers.Add(OpHandlerID.EXDEHL, EXDEHL);
			_handlers.Add(OpHandlerID.LDSPHL, LDSPHL);
			_handlers.Add(OpHandlerID.DI, DI);
			_handlers.Add(OpHandlerID.EI, EI);
			_handlers.Add(OpHandlerID.LDRegisterPair, LDRegisterPair);
			_handlers.Add(OpHandlerID.LDRegisterPairAcc, LDRegisterPairAcc);
			_handlers.Add(OpHandlerID.LDAddressAcc, LDAddressAcc);
			_handlers.Add(OpHandlerID.RLCA, RLCA);
			_handlers.Add(OpHandlerID.RRCA, RRCA);
			_handlers.Add(OpHandlerID.EXAF, EXAF);
			_handlers.Add(OpHandlerID.LDAccRegisterPair, LDAccRegisterPair);
			_handlers.Add(OpHandlerID.LDAccAddress, LDAccAddress);
			_handlers.Add(OpHandlerID.ADDHLRegisterPair, ADDHLRegisterPair);
			_handlers.Add(OpHandlerID.DJNZ, DJNZ);
			_handlers.Add(OpHandlerID.RLA, RLA);
			_handlers.Add(OpHandlerID.RRA, RRA);
			_handlers.Add(OpHandlerID.JR, JR);
			_handlers.Add(OpHandlerID.JRCondition, JRCondition);
			_handlers.Add(OpHandlerID.DAA, DAA);
			_handlers.Add(OpHandlerID.CPL, CPL);
			_handlers.Add(OpHandlerID.CCF, CCF);
			_handlers.Add(OpHandlerID.SCF, SCF);
			_handlers.Add(OpHandlerID.INCHLIndirect, INCHLIndirect);
			_handlers.Add(OpHandlerID.DECHLIndirect, DECHLIndirect);
			_handlers.Add(OpHandlerID.LDHLIndirectImm, LDHLIndirectImm);
			_handlers.Add(OpHandlerID.RST, RST);
			_handlers.Add(OpHandlerID.OUTAcc, OUTAcc);
			_handlers.Add(OpHandlerID.INAcc, INAcc);
			_handlers.Add(OpHandlerID.LDAddressHL, LDAddressHL);
			_handlers.Add(OpHandlerID.LDHLAddress, LDHLAddress);

			// CB prefixed opcode handlers
			_handlers.Add(OpHandlerID.RLCRegister, RLCRegister);
			_handlers.Add(OpHandlerID.RLCHLIndirect, RLCHLIndirect);
			_handlers.Add(OpHandlerID.RRCRegister, RRCRegister);
			_handlers.Add(OpHandlerID.RRCHLIndirect, RRCHLIndirect);
			_handlers.Add(OpHandlerID.RLRegister, RLRegister);
			_handlers.Add(OpHandlerID.RLHLIndirect, RLHLIndirect);
			_handlers.Add(OpHandlerID.RRRegister, RRRegister);
			_handlers.Add(OpHandlerID.RRHLIndirect, RRHLIndirect);
			_handlers.Add(OpHandlerID.SLARegister, SLARegister);
			_handlers.Add(OpHandlerID.SLAHLIndirect, SLAHLIndirect);
			_handlers.Add(OpHandlerID.SRARegister, SRARegister);
			_handlers.Add(OpHandlerID.SRAHLIndirect, SRAHLIndirect);
			_handlers.Add(OpHandlerID.SRLRegister, SRLRegister);
			_handlers.Add(OpHandlerID.SRLHLIndirect, SRLHLIndirect);
			_handlers.Add(OpHandlerID.BITRegister, BITRegister);
			_handlers.Add(OpHandlerID.BITHLIndirect, BITHLIndirect);
			_handlers.Add(OpHandlerID.RESRegister, RESRegister);
			_handlers.Add(OpHandlerID.RESHLIndirect, RESHLIndirect);
			_handlers.Add(OpHandlerID.SETRegister, SETRegister);
			_handlers.Add(OpHandlerID.SETHLIndirect, SETHLIndirect);


			// ED prefixed opcode handlers
			_handlers.Add(OpHandlerID.INRegister, INRegister);
			_handlers.Add(OpHandlerID.OUTRegister, OUTRegister);
			_handlers.Add(OpHandlerID.SBCHLRegisterPair, SBCHLRegisterPair);
			_handlers.Add(OpHandlerID.ADCHLRegisterPair, ADCHLRegisterPair);
			_handlers.Add(OpHandlerID.LDAddressRegisterPair, LDAddressRegisterPair);
			_handlers.Add(OpHandlerID.LDRegisterPairAddress, LDRegisterPairAddress);
			_handlers.Add(OpHandlerID.NEG, NEG);
			_handlers.Add(OpHandlerID.RETN, RETN);
			_handlers.Add(OpHandlerID.InterruptMode0, InterruptMode0);
			_handlers.Add(OpHandlerID.LDIAcc, LDIAcc);
			_handlers.Add(OpHandlerID.RETI, RETI);
			_handlers.Add(OpHandlerID.LDRAcc, LDRAcc);
			_handlers.Add(OpHandlerID.InterruptMode1, InterruptMode1);
			_handlers.Add(OpHandlerID.LDAccI, LDAccI);
			_handlers.Add(OpHandlerID.InterruptMode2, InterruptMode2);
			_handlers.Add(OpHandlerID.LDAccR, LDAccR);
			_handlers.Add(OpHandlerID.RRD, RRD);
			_handlers.Add(OpHandlerID.RLD, RLD);
			_handlers.Add(OpHandlerID.LDIncDec, LDIncDec);
			_handlers.Add(OpHandlerID.CPIncDec, CPIncDec);
			_handlers.Add(OpHandlerID.INIncDec, INIncDec);
			_handlers.Add(OpHandlerID.OUTIncDec, OUTIncDec);
			_handlers.Add(OpHandlerID.LDIncDecRepeat, LDIncDecRepeat);
			_handlers.Add(OpHandlerID.CPIncDecRepeat, CPIncDecRepeat);
			_handlers.Add(OpHandlerID.INIncDecRepeat, INIncDecRepeat);
			_handlers.Add(OpHandlerID.OUTIncDecRepeat, OUTIncDecRepeat);

			// DD prefixed opcode handlers
			_handlers.Add(OpHandlerID.ADDIXRegisterPair, ADDIXRegisterPair);
			_handlers.Add(OpHandlerID.LDIXAddress, LDIXAddress);
			_handlers.Add(OpHandlerID.LDAddressIndirectIX, LDAddressIndirectIX);
			_handlers.Add(OpHandlerID.INCIX, INCIX);
			_handlers.Add(OpHandlerID.LDIXAddressIndirect, LDIXAddressIndirect);
			_handlers.Add(OpHandlerID.DECIX, DECIX);
			_handlers.Add(OpHandlerID.POPIX, POPIX);
			_handlers.Add(OpHandlerID.EXSPIX, EXSPIX);
			_handlers.Add(OpHandlerID.PUSHIX, PUSHIX);
			_handlers.Add(OpHandlerID.JPIX, JPIX);
			_handlers.Add(OpHandlerID.LDSPIX, LDSPIX);

			// FD prefixed opcode handlers
			_handlers.Add(OpHandlerID.ADDIYRegisterPair, ADDIYRegisterPair);
			_handlers.Add(OpHandlerID.LDIYAddress, LDIYAddress);
			_handlers.Add(OpHandlerID.LDAddressIndirectIY, LDAddressIndirectIY);
			_handlers.Add(OpHandlerID.INCIY, INCIY);
			_handlers.Add(OpHandlerID.LDIYAddressIndirect, LDIYAddressIndirect);
			_handlers.Add(OpHandlerID.DECIY, DECIY);
			_handlers.Add(OpHandlerID.POPIY, POPIY);
			_handlers.Add(OpHandlerID.EXSPIY, EXSPIY);
			_handlers.Add(OpHandlerID.PUSHIY, PUSHIY);
			_handlers.Add(OpHandlerID.JPIY, JPIY);
			_handlers.Add(OpHandlerID.LDSPIY, LDSPIY);

		}

		/// <summary>
		/// Execute the specified instruction.
		/// </summary>
		/// <remarks>
		/// This method implements special handling for 0xDD, 0xDDCB, 0xFD, 0xFDBC instructions that correspond to (IX+d) and (IY+d)
		/// instructions that can be mapped to their (HL) equivalents - e.g. LD B,(IX+d) can use the handler for LD B,(HL) because the
		/// handler for that instruction can determine that IX+d should be used instead of HL.
		/// </remarks>
		/// <param name="instruction">The <see cref="Instruction"/> instance of the instruction to be executed.</param>
		internal void Execute(Instruction instruction)
		{
			OpHandlerID handlerID = OpHandlerID.IXIYIndirect;
			if (instruction.Info.HandlerID == OpHandlerID.IXIYIndirect)
			{
				if (instruction.Prefix == OpcodePrefix.DD || instruction.Prefix == OpcodePrefix.FD)
				{
					// Use handler from Unprefixed_Instructions table - i.e. use the corresponding (HL) handler for (IX+d) and (IY+d) instructions.
					InstructionInfo info = InstructionTables.Unprefixed_Instructions[instruction.Opcode];
					handlerID = info.HandlerID;
				}
				else if (instruction.Prefix == OpcodePrefix.DDCB || instruction.Prefix == OpcodePrefix.FDCB)
				{
					// Use handler from CBPrefixed_Instructions table - i.e. use the corresponding (HL) handler for (IX+d) and (IY+d) instructions.
					InstructionInfo info = InstructionTables.CBPrefixed_Instructions[instruction.Opcode];
					handlerID = info.HandlerID;
				}
			}
			else
			{
				handlerID = instruction.Info.HandlerID;
			}

			if (_handlers.ContainsKey(handlerID))
			{
				_handlers[handlerID]?.Invoke(instruction);          // Call the handler Action.
			}
		}

		// **************
		// Helper methods
		// **************

		/// <summary>
		/// Perform an 8-bit addition (with or without Carry), setting the flags accordingly.
		/// </summary>
		/// <param name="x">The source value.</param>
		/// <param name="y">The value to be added to the source value.</param>
		/// <param name="addCarry">If <c>true</c> then the current state of the carry flag is included in the addition, otherwise it is not.</param>
		/// <returns>The result of the addition operation.</returns>
		internal byte Add8Bit(byte x, byte y, bool addCarry)
		{
			var add = y + (addCarry && Machine.CPU.CarryFlag ? 1 : 0);
			var result = x + add;

			if ((((x ^ add) & 0x80) == 0) && (((x ^ result) & 0x80) != 0))
			{
				Machine.CPU.ParityOverflowFlag = true;
			}
			else
			{
				Machine.CPU.ParityOverflowFlag = false;
			}

			Machine.CPU.CarryFlag = result > 0xFF;
			Machine.CPU.HalfCarryFlag = ((x & 0x0F) + (add & 0x0F)) > 0x0F;
			Machine.CPU.SubtractFlag = false;
			Machine.CPU.ZeroFlag = (byte)result == 0;
			Machine.CPU.SignFlag = ((byte)result & 0x80) == 0x80;

			return (byte)result;
		}

		/// <summary>
		/// Perform an 16-bit addition (with or without Carry), setting the flags accordingly.
		/// </summary>
		/// <param name="x">The source value.</param>
		/// <param name="y">The value to be added to the source value.</param>
		/// <param name="addCarry">If <c>true</c> then the current state of the carry flag is included in the addition, otherwise it is not.</param>
		/// <returns>The result of the addition operation.</returns>
		internal ushort Add16Bit(ushort x, ushort y, bool addCarry)
		{
			var add = y + (addCarry && Machine.CPU.CarryFlag ? 1 : 0);
			var result = x + add;

			if (addCarry)
			{
				if ((((x ^ add) & 0x8000) == 0) && (((x ^ result) & 0x8000) != 0))
				{
					Machine.CPU.ParityOverflowFlag = true;
				}
				else
				{
					Machine.CPU.ParityOverflowFlag = false;
				}
			}

			Machine.CPU.CarryFlag = result > 0xFFFF;
			Machine.CPU.HalfCarryFlag = ((x & 0x0F00) + (add & 0x0F00)) > 0x0F00;
			Machine.CPU.SubtractFlag = false;
			if (addCarry)
			{
				Machine.CPU.ZeroFlag = (ushort)result == 0;
				Machine.CPU.SignFlag = ((ushort)result & 0x8000) == 0x8000;
			}

			return (ushort)result;
		}

		/// <summary>
		/// Perform an 8-bit subtraction (with or without borrow), setting the flags accordingly.
		/// </summary>
		/// <param name="x">The source value.</param>
		/// <param name="y">The value to be subtracted from the source value.</param>
		/// <param name="subCarry">If <c>true</c> then the current state of the carry flag is included in the subtraction (i.e. a borrow), otherwise it is not.</param>
		/// <returns>The result of the subtraction operation.</returns>
		internal byte Subtract8Bit(byte x, byte y, bool subCarry)
		{
			var sub = y + (subCarry && Machine.CPU.CarryFlag ? 1 : 0);
			var result = x - sub;

			if ((((x ^ sub) & 0x80) != 0) && (((sub ^ result) & 0x80) == 0))
			{
				Machine.CPU.ParityOverflowFlag = true;
			}
			else
			{
				Machine.CPU.ParityOverflowFlag = false;
			}

			Machine.CPU.CarryFlag = result < 0;
			Machine.CPU.HalfCarryFlag = (x & 0x0F) < (sub & 0x0F);
			Machine.CPU.SubtractFlag = true;
			Machine.CPU.ZeroFlag = (byte)result == 0;
			Machine.CPU.SignFlag = ((byte)result & 0x80) == 0x80;

			return (byte)result;
		}

		/// <summary>
		/// Perform an 16-bit subtraction (with or without borrow), setting the flags accordingly.
		/// </summary>
		/// <param name="x">The source value.</param>
		/// <param name="y">The value to be subtracted from the source value.</param>
		/// <param name="subCarry">If <c>true</c> then the current state of the carry flag is included in the subtraction (i.e. a borrow), otherwise it is not.</param>
		/// <returns>The result of the subtraction operation.</returns>
		internal ushort Subtract16Bit(ushort x, ushort y, bool subCarry)
		{
			var sub = y + (subCarry && Machine.CPU.CarryFlag ? 1 : 0);
			var result = x - sub;

			if ((((x ^ sub) & 0x8000) != 0) && (((sub ^ result) & 0x8000) == 0))
			{
				Machine.CPU.ParityOverflowFlag = true;
			}
			else
			{
				Machine.CPU.ParityOverflowFlag = false;
			}

			Machine.CPU.CarryFlag = result < 0;
			Machine.CPU.HalfCarryFlag = (x & 0x0FFF) < (sub & 0x0FFF);
			Machine.CPU.SubtractFlag = true;
			Machine.CPU.ZeroFlag = (ushort)result == 0;
			Machine.CPU.SignFlag = ((ushort)result & 0x8000) == 0x8000;

			return (ushort)result;
		}

		/// <summary>
		/// Set the state of the flags following a logic operation.
		/// </summary>
		/// <param name="value">The value whose state is to be checked.</param>
		/// <param name="halfCarry">The new state of the Half Carry flag.</param>
		internal void SetLogicOperationFlags(byte value, bool halfCarry)
		{
			Machine.CPU.CarryFlag = false;
			Machine.CPU.SubtractFlag = false;
			Machine.CPU.ZeroFlag = value == 0x00;
			Machine.CPU.SignFlag = (value & 0x80) == 0x80;
			Machine.CPU.ParityOverflowFlag = Helpers.Parity(value);
			Machine.CPU.HalfCarryFlag = halfCarry;
		}

		/// <summary>
		/// Set the state of the flags following a comparison operation.
		/// </summary>
		/// <param name="diff">The difference between the Accumulator and the value it is being compared with.</param>
		/// <param name="comparedWith">The value with which the Accumulator is being compared.</param>
		internal void SetCompareOperationFlags(byte diff, byte comparedWith)
		{
			Machine.CPU.SubtractFlag = true;
			Machine.CPU.CarryFlag = comparedWith > Machine.CPU.A;
			Machine.CPU.ZeroFlag = diff == 0x00;
			Machine.CPU.SignFlag = (diff & 0x80) == 0x80;
			Machine.CPU.HalfCarryFlag = (Machine.CPU.A & 0x0F) < (comparedWith & 0x0F);

			if ((((Machine.CPU.A ^ comparedWith) & 0x80) != 0) && (((comparedWith ^ diff) & 0x80) == 0))
			{
				Machine.CPU.ParityOverflowFlag = true;
			}
			else
			{
				Machine.CPU.ParityOverflowFlag = false;
			}
		}

		/// <summary>
		/// Set the state of the flags following an increment or decrement operation.
		/// </summary>
		/// <param name="value">The new (i.e. incremented/decremented) value.</param>
		/// <param name="prevValue">The previous (unincremented/undecremented) value.</param>
		/// <param name="decrement"><c>true</c> if the flags are being set for a decrement operation or <c>false</c> for an increment operation.</param>
		internal void SetIncDecOperationFlags(byte value, byte prevValue, bool decrement)
		{
			Machine.CPU.SubtractFlag = decrement;   // true for DEC operation, false for INC operation
			Machine.CPU.ZeroFlag = value == 0x00;
			Machine.CPU.SignFlag = (value & 0x80) == 0x80;
			Machine.CPU.HalfCarryFlag = decrement ? (prevValue & 0x0F) < 0x01 : ((prevValue & 0x0F) + 0x01) > 0x0F;
			Machine.CPU.ParityOverflowFlag = decrement ? prevValue == 0x80 : prevValue == 0x7F;
		}

		/// <summary>
		/// Set the state of the flags following shift or rotate operation.
		/// </summary>
		/// <param name="value">The shifted/rotated value.</param>
		internal void SetShiftRotateOperationFlags(byte value)
		{
			Machine.CPU.SubtractFlag = false;
			Machine.CPU.ZeroFlag = value == 0x00;
			Machine.CPU.SignFlag = (value & 0x80) == 0x80;
			Machine.CPU.HalfCarryFlag = false;
			Machine.CPU.ParityOverflowFlag = Helpers.Parity(value);
		}

		/// <summary>
		/// Gets the absolute address for the specified instruction.
		/// </summary>
		/// <remarks>
		/// If the instruction uses one of the index registers (IX or IY) then this method returns
		/// the index plus the displacement value (i.e. IX+d or IY+d); otherwise, the value in the HL register
		/// is returned.
		/// This allows us to reuse the HL indirect methods for indexed indirect methods -
		/// e.g. reusing LD (HL),n for LD (IX+d),n and for LD (IY+d),n
		/// </remarks>
		/// <param name="inst">The instruction currently being executed.</param>
		/// <returns>The calculated absolute address.</returns>
		internal ushort GetIndexedAddress(Instruction inst)
		{
			return inst.Prefix switch
			{
				OpcodePrefix.DD => (ushort)(Machine.CPU.IX + (sbyte)inst.Displacement),
				OpcodePrefix.DDCB => (ushort)(Machine.CPU.IX + (sbyte)inst.Displacement),
				OpcodePrefix.FD => (ushort)(Machine.CPU.IY + (sbyte)inst.Displacement),
				OpcodePrefix.FDCB => (ushort)(Machine.CPU.IY + (sbyte)inst.Displacement),
				_ => Machine.CPU.HL,
			};
		}


		// *************************
		//
		// Operation handler methods
		//
		// *************************

		// Unprefixed instructions

		private void NOP(Instruction inst)
		{
			// No operation performed for NOP (obviously!).
		}

		private void LDRegister(Instruction inst)
		{
			byte srcValue = 0;
			var destReg = (inst.Opcode & 0x38) >> 3;
			switch (inst.Info.AddrMode1)
			{
				case AddrMode.Register:
					var srcReg = inst.Opcode & 0x07;
					srcValue = Machine.CPU.ReadRegister((byte)srcReg);
					break;
				case AddrMode.Immediate:
					srcValue = inst.ByteOperand;
					break;
			}

			Machine.CPU.WriteRegister((byte)destReg, srcValue);
		}

		private void LDRegisterHL(Instruction inst)
		{
			var destReg = (inst.Opcode & 0x38) >> 3;
			ushort address = GetIndexedAddress(inst);
			byte srcValue = Machine.Memory.Read(address);
			Machine.CPU.WriteRegister((byte)destReg, srcValue);
		}

		private void LDHLRegister(Instruction inst)
		{
			var srcReg = inst.Opcode & 0x07;
			byte srcValue = Machine.CPU.ReadRegister((byte)srcReg);
			ushort address = GetIndexedAddress(inst);
			Machine.Memory.Write(address, srcValue);
		}

		private void ADDAccRegister(Instruction inst)
		{
			byte srcValue = 0;
			switch (inst.Info.AddrMode1)
			{
				case AddrMode.Register:
					var srcReg = inst.Opcode & 0x07;
					srcValue = Machine.CPU.ReadRegister((byte)srcReg);
					break;
				case AddrMode.Immediate:
					srcValue = inst.ByteOperand;
					break;
			}
			Machine.CPU.A = Add8Bit(Machine.CPU.A, srcValue, false);
		}

		private void ADDAccHL(Instruction inst)
		{
			ushort address = GetIndexedAddress(inst);
			byte srcValue = Machine.Memory.Read(address);
			Machine.CPU.A = Add8Bit(Machine.CPU.A, srcValue, false);
		}

		private void ADCAccRegister(Instruction inst)
		{
			byte srcValue = 0;
			switch (inst.Info.AddrMode1)
			{
				case AddrMode.Register:
					var srcReg = inst.Opcode & 0x07;
					srcValue = Machine.CPU.ReadRegister((byte)srcReg);
					break;
				case AddrMode.Immediate:
					srcValue = inst.ByteOperand;
					break;
			}
			Machine.CPU.A = Add8Bit(Machine.CPU.A, srcValue, true);
		}

		private void ADCAccHL(Instruction inst)
		{
			ushort address = GetIndexedAddress(inst);
			byte srcValue = Machine.Memory.Read(address);
			Machine.CPU.A = Add8Bit(Machine.CPU.A, srcValue, true);
		}

		private void SUBAccRegister(Instruction inst)
		{
			byte srcValue = 0;
			switch (inst.Info.AddrMode1)
			{
				case AddrMode.Register:
					var srcReg = inst.Opcode & 0x07;
					srcValue = Machine.CPU.ReadRegister((byte)srcReg);
					break;
				case AddrMode.Immediate:
					srcValue = inst.ByteOperand;
					break;
			}
			Machine.CPU.A = Subtract8Bit(Machine.CPU.A, srcValue, false);
		}

		private void SUBAccHL(Instruction inst)
		{
			ushort address = GetIndexedAddress(inst);
			byte srcValue = Machine.Memory.Read(address);
			Machine.CPU.A = Subtract8Bit(Machine.CPU.A, srcValue, false);
		}

		private void SBCAccRegister(Instruction inst)
		{
			byte srcValue = 0;
			switch (inst.Info.AddrMode1)
			{
				case AddrMode.Register:
					var srcReg = inst.Opcode & 0x07;
					srcValue = Machine.CPU.ReadRegister((byte)srcReg);
					break;
				case AddrMode.Immediate:
					srcValue = inst.ByteOperand;
					break;
			}
			Machine.CPU.A = Subtract8Bit(Machine.CPU.A, srcValue, true);
		}

		private void SBCAccHL(Instruction inst)
		{
			ushort address = GetIndexedAddress(inst);
			byte srcValue = Machine.Memory.Read(address);
			Machine.CPU.A = Subtract8Bit(Machine.CPU.A, srcValue, true);
		}

		private void ANDRegister(Instruction inst)
		{
			byte srcValue = 0;
			switch (inst.Info.AddrMode1)
			{
				case AddrMode.Register:
					var srcReg = inst.Opcode & 0x07;
					srcValue = Machine.CPU.ReadRegister((byte)srcReg);
					break;
				case AddrMode.Immediate:
					srcValue = inst.ByteOperand;
					break;
			}
			Machine.CPU.A &= srcValue;
			SetLogicOperationFlags(Machine.CPU.A, true);
		}

		private void ANDHL(Instruction inst)
		{
			ushort address = GetIndexedAddress(inst);
			byte srcValue = Machine.Memory.Read(address);
			Machine.CPU.A &= srcValue;
			SetLogicOperationFlags(Machine.CPU.A, true);
		}

		private void XORRegister(Instruction inst)
		{
			byte srcValue = 0;
			switch (inst.Info.AddrMode1)
			{
				case AddrMode.Register:
					var srcReg = inst.Opcode & 0x07;
					srcValue = Machine.CPU.ReadRegister((byte)srcReg);
					break;
				case AddrMode.Immediate:
					srcValue = inst.ByteOperand;
					break;
			}
			Machine.CPU.A ^= srcValue;
			SetLogicOperationFlags(Machine.CPU.A, false);
		}

		private void XORHL(Instruction inst)
		{
			ushort address = GetIndexedAddress(inst);
			byte srcValue = Machine.Memory.Read(address);
			Machine.CPU.A ^= srcValue;
			SetLogicOperationFlags(Machine.CPU.A, false);
		}

		private void ORRegister(Instruction inst)
		{
			byte srcValue = 0;
			switch (inst.Info.AddrMode1)
			{
				case AddrMode.Register:
					var srcReg = inst.Opcode & 0x07;
					srcValue = Machine.CPU.ReadRegister((byte)srcReg);
					break;
				case AddrMode.Immediate:
					srcValue = inst.ByteOperand;
					break;
			}
			Machine.CPU.A |= srcValue;
			SetLogicOperationFlags(Machine.CPU.A, false);
		}

		private void ORHL(Instruction inst)
		{
			ushort address = GetIndexedAddress(inst);
			byte srcValue = Machine.Memory.Read(address);
			Machine.CPU.A |= srcValue;
			SetLogicOperationFlags(Machine.CPU.A, false);
		}

		private void CPRegister(Instruction inst)
		{
			byte srcValue = 0;
			switch (inst.Info.AddrMode1)
			{
				case AddrMode.Register:
					var srcReg = inst.Opcode & 0x07;
					srcValue = Machine.CPU.ReadRegister((byte)srcReg);
					break;
				case AddrMode.Immediate:
					srcValue = inst.ByteOperand;
					break;
			}
			var diff = Machine.CPU.A - srcValue;
			SetCompareOperationFlags((byte)diff, srcValue);
		}

		private void CPHL(Instruction inst)
		{
			ushort address = GetIndexedAddress(inst);
			byte srcValue = Machine.Memory.Read(address);
			var diff = Machine.CPU.A - srcValue;
			SetCompareOperationFlags((byte)diff, srcValue);
		}

		private void INCRegister(Instruction inst)
		{
			var reg = (byte)((inst.Opcode & 0x38) >> 3);
			byte srcValue = Machine.CPU.ReadRegister(reg);
			var newValue = (byte)(srcValue + 1);
			Machine.CPU.WriteRegister(reg, newValue);
			SetIncDecOperationFlags(newValue, srcValue, false);
		}

		private void DECRegister(Instruction inst)
		{
			var reg = (byte)((inst.Opcode & 0x38) >> 3);
			byte srcValue = Machine.CPU.ReadRegister(reg);
			var newValue = (byte)(srcValue - 1);
			Machine.CPU.WriteRegister(reg, newValue);
			SetIncDecOperationFlags(newValue, srcValue, true);
		}

		private void INCRegisterPair(Instruction inst)
		{
			var regPair = (byte)((inst.Opcode & 0x30) >> 4);
			ushort srcValue = Machine.CPU.ReadRegisterPair(regPair);
			var newValue = (ushort)(srcValue + 1);
			Machine.CPU.WriteRegisterPair(regPair, newValue);
			// NOTE: No flags affected
		}

		private void DECRegisterPair(Instruction inst)
		{
			var regPair = (byte)((inst.Opcode & 0x30) >> 4);
			ushort srcValue = Machine.CPU.ReadRegisterPair(regPair);
			var newValue = (ushort)(srcValue - 1);
			Machine.CPU.WriteRegisterPair(regPair, newValue);
			// NOTE: No flags affected
		}

		private void HALT(Instruction inst)
		{
			// Decrement Program Counter - Will cause this instruction to be executed again (effectively suspending the CPU
			// until a subsequent interrupt or reset is received to break out of the HALT cycle).
			Machine.CPU.PC--;
			Machine.CPU.IsHalted = true;
		}

		private void RETCondition(Instruction inst)
		{
			byte condition = (byte)((inst.Opcode & 0x38) >> 3);
			if (Machine.CPU.EvaluateCondition(condition))
			{
				Machine.CPU.PC = Machine.Stack.Pop();

				// if no CALL instruction has been executed then this RET marks the termination of the code execution.
				if (_numberOfCalls == 0)
				{
					Machine.IsEndOfExecution = true;
					return;
				}
			}
		}

		private void JPCondition(Instruction inst)
		{
			byte condition = (byte)((inst.Opcode & 0x38) >> 3);
			if (Machine.CPU.EvaluateCondition(condition))
			{
				Machine.CPU.PC = inst.WordOperand;
			}
		}

		private void CALLCondition(Instruction inst)
		{
			byte condition = (byte)((inst.Opcode & 0x38) >> 3);
			if (Machine.CPU.EvaluateCondition(condition))
			{
				Machine.Stack.Push(Machine.CPU.PC);
				Machine.CPU.PC = inst.WordOperand;
				_numberOfCalls++;
			}
		}

		private void POPRegisterPair(Instruction inst)
		{
			var regPair = (byte)((inst.Opcode & 0x30) >> 4);
			ushort popped = Machine.Stack.Pop();
			Machine.CPU.WriteRegisterPair(regPair, popped, true);
		}

		private void JP(Instruction inst)
		{
			Machine.CPU.PC = inst.WordOperand;
		}

		private void PUSHRegisterPair(Instruction inst)
		{
			var regPair = (byte)((inst.Opcode & 0x30) >> 4);
			ushort srcValue = Machine.CPU.ReadRegisterPair(regPair, true);
			Machine.Stack.Push(srcValue);
		}

		private void RET(Instruction inst)
		{
			Machine.CPU.PC = Machine.Stack.Pop();

			// if no CALL instruction has been executed then this RET marks the termination of the code execution.
			if (_numberOfCalls == 0)
			{
				Machine.IsEndOfExecution = true;
				return;
			}

			_numberOfCalls--;

		}

		private void CALL(Instruction inst)
		{
			Machine.Stack.Push(Machine.CPU.PC);
			Machine.CPU.PC = inst.WordOperand;
			_numberOfCalls++;
		}

		private void EXX(Instruction inst)
		{
			Machine.CPU.ExchangeRegPairsWithShadow();
		}

		private void EXSPHL(Instruction inst)
		{
			ushort value = Machine.Stack.Pop();
			Machine.Stack.Push(Machine.CPU.HL);
			Machine.CPU.HL = value;
		}

		private void JPHL(Instruction inst)
		{
			Machine.CPU.PC = Machine.CPU.HL;
		}

		private void EXDEHL(Instruction inst)
		{
			ushort temp = Machine.CPU.HL;
			Machine.CPU.HL = Machine.CPU.DE;
			Machine.CPU.DE = temp;
		}

		private void LDSPHL(Instruction inst)
		{
			Machine.CPU.SP = Machine.CPU.HL;
		}

		private void DI(Instruction inst)
		{
			Machine.CPU.IFF1 = Machine.CPU.IFF2 = false;
		}

		private void EI(Instruction inst)
		{
			Machine.CPU.IFF1 = Machine.CPU.IFF2 = true;
		}

		private void LDRegisterPair(Instruction inst)
		{
			var regPair = (byte)((inst.Opcode & 0x30) >> 4);
			Machine.CPU.WriteRegisterPair(regPair, inst.WordOperand);
		}

		private void LDRegisterPairAcc(Instruction inst)
		{
			var regPair = (byte)((inst.Opcode & 0x30) >> 4);
			var regValue = Machine.CPU.ReadRegisterPair(regPair);
			Machine.Memory.Write(regValue, Machine.CPU.A);
		}

		private void LDAddressAcc(Instruction inst)
		{
			Machine.Memory.Write(inst.WordOperand, Machine.CPU.A);
		}

		private void RLCA(Instruction inst)
		{
			bool newCarry = (Machine.CPU.A & 0x80) == 0x80;
			Machine.CPU.A = (byte)((Machine.CPU.A << 1) + (byte)(newCarry ? 0x01 : 0));
			Machine.CPU.CarryFlag = newCarry;
			Machine.CPU.HalfCarryFlag = false;
			Machine.CPU.SubtractFlag = false;
		}

		private void RRCA(Instruction inst)
		{
			bool newCarry = (Machine.CPU.A & 0x01) == 0x01;
			Machine.CPU.A = (byte)((Machine.CPU.A >> 1) + (byte)(newCarry ? 0x80 : 0));
			Machine.CPU.CarryFlag = newCarry;
			Machine.CPU.HalfCarryFlag = false;
			Machine.CPU.SubtractFlag = false;
		}

		private void EXAF(Instruction inst)
		{
			Machine.CPU.ExchangeAFWithShadowAF();
		}

		private void LDAccRegisterPair(Instruction inst)
		{
			var regPair = (byte)((inst.Opcode & 0x30) >> 4);
			var regValue = Machine.CPU.ReadRegisterPair(regPair);
			Machine.CPU.A = Machine.Memory.Read(regValue);
		}

		private void LDAccAddress(Instruction inst)
		{
			Machine.CPU.A = Machine.Memory.Read(inst.WordOperand);
		}

		private void ADDHLRegisterPair(Instruction inst)
		{
			var regPair = (byte)((inst.Opcode & 0x30) >> 4);
			var regValue = Machine.CPU.ReadRegisterPair(regPair);
			Machine.CPU.HL += regValue;
		}

		private void DJNZ(Instruction inst)
		{
			Machine.CPU.B--;
			if (Machine.CPU.B != 0)
			{
				Machine.CPU.PC = (ushort)(Machine.CPU.PC + (sbyte)inst.Displacement);
			}
		}

		private void RLA(Instruction inst)
		{
			bool newCarry = (Machine.CPU.A & 0x80) == 0x80;
			Machine.CPU.A = (byte)((Machine.CPU.A << 1) + (byte)(Machine.CPU.CarryFlag ? 0x01 : 0));
			Machine.CPU.CarryFlag = newCarry;
			Machine.CPU.HalfCarryFlag = false;
			Machine.CPU.SubtractFlag = false;
		}

		private void RRA(Instruction inst)
		{
			bool newCarry = (Machine.CPU.A & 0x01) == 0x01;
			Machine.CPU.A = (byte)((Machine.CPU.A >> 1) + (byte)(Machine.CPU.CarryFlag ? 0x80 : 0));
			Machine.CPU.CarryFlag = newCarry;
			Machine.CPU.HalfCarryFlag = false;
			Machine.CPU.SubtractFlag = false;
		}

		private void JR(Instruction inst)
		{
			Machine.CPU.PC = (ushort)(Machine.CPU.PC + (sbyte)inst.Displacement);
		}

		private void JRCondition(Instruction inst)
		{
			byte condition = (byte)((inst.Opcode & 0x18) >> 3);     // AND with 0x18 because there are only 4 possible conditions here - NZ, Z, NC, C
			if (Machine.CPU.EvaluateCondition(condition))
			{
				Machine.CPU.PC = (ushort)(Machine.CPU.PC + (sbyte)inst.Displacement);
			}
		}

		private void DAA(Instruction inst)
		{
			byte prevA = Machine.CPU.A;
			byte correctionFactor = 0x00;
			if (Machine.CPU.A > 0x99 || Machine.CPU.CarryFlag)
			{
				correctionFactor += 0x60;
				Machine.CPU.CarryFlag = true;
			}
			else
			{
				Machine.CPU.CarryFlag = false;
			}
			if ((Machine.CPU.A & 0x0F) > 0x09 || Machine.CPU.HalfCarryFlag)
			{
				correctionFactor += 0x06;
			}

			if (Machine.CPU.SubtractFlag)
			{
				Machine.CPU.A -= correctionFactor;
			}
			else
			{
				Machine.CPU.A += correctionFactor;
			}

			Machine.CPU.SignFlag = (Machine.CPU.A & 0x80) == 0x80;
			Machine.CPU.ZeroFlag = Machine.CPU.A == 0x00;
			Machine.CPU.HalfCarryFlag = ((prevA & 0x10) ^ (Machine.CPU.A & 0x10)) == 0x10;
			Machine.CPU.ParityOverflowFlag = Helpers.Parity(Machine.CPU.A);
		}

		private void CPL(Instruction inst)
		{
			Machine.CPU.A ^= 0xFF;
			Machine.CPU.SubtractFlag = true;
			Machine.CPU.HalfCarryFlag = true;
		}

		private void CCF(Instruction inst)
		{
			Machine.CPU.HalfCarryFlag = Machine.CPU.CarryFlag;      // Half Carry set to current Carry
			Machine.CPU.CarryFlag = !Machine.CPU.CarryFlag;         // Toggle Carry flag.
			Machine.CPU.SubtractFlag = false;                       // Subtract flag is cleared by CCF
		}

		private void SCF(Instruction inst)
		{
			Machine.CPU.CarryFlag = true;
			Machine.CPU.HalfCarryFlag = false;
			Machine.CPU.SubtractFlag = false;
		}

		private void INCHLIndirect(Instruction inst)
		{
			ushort address = GetIndexedAddress(inst);
			byte value = Machine.Memory.Read(address);
			byte prevValue = value;
			value++;
			Machine.Memory.Write(address, value);
			SetIncDecOperationFlags(value, prevValue, false);
		}


		private void DECHLIndirect(Instruction inst)
		{
			ushort address = GetIndexedAddress(inst);
			byte value = Machine.Memory.Read(address);
			byte prevValue = value;
			value--;
			Machine.Memory.Write(address, value);
			SetIncDecOperationFlags(value, prevValue, true);
		}

		private void LDHLIndirectImm(Instruction inst)
		{
			ushort address = GetIndexedAddress(inst);
			Machine.Memory.Write(address, inst.ByteOperand);
		}

		private void RST(Instruction inst)
		{
			byte value = (byte)((inst.Opcode & 0x38) >> 3);
			ushort address = Machine.CPU.GetPageZeroAddress(value);
			Machine.Stack.Push(Machine.CPU.PC);
			Machine.CPU.PC = address;
		}

		private void OUTAcc(Instruction inst)
		{
			var portAddr = (ushort)((Machine.CPU.A << 8) + inst.ByteOperand);
			Machine.Port.Write(portAddr, Machine.CPU.A);
		}

		private void INAcc(Instruction inst)
		{
			var portAddr = (ushort)((Machine.CPU.A << 8) + inst.ByteOperand);
			Machine.CPU.A = Machine.Port.Read(portAddr);
		}

		private void LDAddressHL(Instruction inst)
		{
			Machine.Memory.Write(inst.WordOperand, Machine.CPU.L);
			Machine.Memory.Write((ushort)(inst.WordOperand + 1), Machine.CPU.H);
		}

		private void LDHLAddress(Instruction inst)
		{
			Machine.CPU.L = Machine.Memory.Read(inst.WordOperand);
			Machine.CPU.H = Machine.Memory.Read((ushort)(inst.WordOperand + 1));
		}


		// CB prefixed opcodes

		private void RLCRegister(Instruction inst)
		{
			byte reg = (byte)(inst.Opcode & 0x07);
			byte val = Machine.CPU.ReadRegister(reg);
			bool newCarry = (val & 0x80) == 0x80;
			val = (byte)((val << 1) + (byte)(newCarry ? 0x01 : 0));
			Machine.CPU.WriteRegister(reg, val);
			Machine.CPU.CarryFlag = newCarry;
			SetShiftRotateOperationFlags(val);
		}

		private void RLCHLIndirect(Instruction inst)
		{
			ushort address = GetIndexedAddress(inst);
			byte val = Machine.Memory.Read(address);
			bool newCarry = (val & 0x80) == 0x80;
			val = (byte)((val << 1) + (byte)(newCarry ? 0x01 : 0));
			Machine.Memory.Write(address, val);
			Machine.CPU.CarryFlag = newCarry;
			SetShiftRotateOperationFlags(val);
		}

		private void RRCRegister(Instruction inst)
		{
			byte reg = (byte)(inst.Opcode & 0x07);
			byte val = Machine.CPU.ReadRegister(reg);
			bool newCarry = (val & 0x01) == 0x01;
			val = (byte)((val >> 1) + (byte)(newCarry ? 0x80 : 0));
			Machine.CPU.WriteRegister(reg, val);
			Machine.CPU.CarryFlag = newCarry;
			SetShiftRotateOperationFlags(val);
		}

		private void RRCHLIndirect(Instruction inst)
		{
			ushort address = GetIndexedAddress(inst);
			byte val = Machine.Memory.Read(address);
			bool newCarry = (val & 0x01) == 0x01;
			val = (byte)((val >> 1) + (byte)(newCarry ? 0x80 : 0));
			Machine.Memory.Write(address, val);
			Machine.CPU.CarryFlag = newCarry;
			SetShiftRotateOperationFlags(val);
		}

		private void RLRegister(Instruction inst)
		{
			byte reg = (byte)(inst.Opcode & 0x07);
			byte val = Machine.CPU.ReadRegister(reg);
			bool newCarry = (val & 0x80) == 0x80;
			val = (byte)((val << 1) + (byte)(Machine.CPU.CarryFlag ? 0x01 : 0));
			Machine.CPU.WriteRegister(reg, val);
			Machine.CPU.CarryFlag = newCarry;
			SetShiftRotateOperationFlags(val);
		}

		private void RLHLIndirect(Instruction inst)
		{
			ushort address = GetIndexedAddress(inst);
			byte val = Machine.Memory.Read(address);
			bool newCarry = (val & 0x80) == 0x80;
			val = (byte)((val << 1) + (byte)(Machine.CPU.CarryFlag ? 0x01 : 0));
			Machine.Memory.Write(address, val);
			Machine.CPU.CarryFlag = newCarry;
			SetShiftRotateOperationFlags(val);
		}

		private void RRRegister(Instruction inst)
		{
			byte reg = (byte)(inst.Opcode & 0x07);
			byte val = Machine.CPU.ReadRegister(reg);
			bool newCarry = (val & 0x01) == 0x01;
			val = (byte)((val >> 1) + (byte)(Machine.CPU.CarryFlag ? 0x80 : 0));
			Machine.CPU.WriteRegister(reg, val);
			Machine.CPU.CarryFlag = newCarry;
			SetShiftRotateOperationFlags(val);
		}

		private void RRHLIndirect(Instruction inst)
		{
			ushort address = GetIndexedAddress(inst);
			byte val = Machine.Memory.Read(address);
			bool newCarry = (val & 0x01) == 0x01;
			val = (byte)((val >> 1) + (byte)(Machine.CPU.CarryFlag ? 0x80 : 0));
			Machine.Memory.Write(address, val);
			Machine.CPU.CarryFlag = newCarry;
			SetShiftRotateOperationFlags(val);
		}

		private void SLARegister(Instruction inst)
		{
			byte reg = (byte)(inst.Opcode & 0x07);
			byte val = Machine.CPU.ReadRegister(reg);
			Machine.CPU.CarryFlag = (val & 0x80) == 0x80;
			val = (byte)(val << 1);
			Machine.CPU.WriteRegister(reg, val);
			SetShiftRotateOperationFlags(val);
		}

		private void SLAHLIndirect(Instruction inst)
		{
			ushort address = GetIndexedAddress(inst);
			byte val = Machine.Memory.Read(address);
			Machine.CPU.CarryFlag = (val & 0x80) == 0x80;
			val = (byte)(val << 1);
			Machine.Memory.Write(address, val);
			SetShiftRotateOperationFlags(val);
		}

		private void SRARegister(Instruction inst)
		{
			byte reg = (byte)(inst.Opcode & 0x07);
			byte val = Machine.CPU.ReadRegister(reg);
			bool newCarry = (val & 0x01) == 0x01;
			byte bit7 = (byte)(val & 0x80);
			val = (byte)((val >> 1) + bit7);
			Machine.CPU.WriteRegister(reg, val);
			Machine.CPU.CarryFlag = newCarry;
			SetShiftRotateOperationFlags(val);
		}

		private void SRAHLIndirect(Instruction inst)
		{
			ushort address = GetIndexedAddress(inst);
			byte val = Machine.Memory.Read(address);
			bool newCarry = (val & 0x01) == 0x01;
			byte bit7 = (byte)(val & 0x80);
			val = (byte)((val >> 1) + bit7);
			Machine.Memory.Write(address, val);
			Machine.CPU.CarryFlag = newCarry;
			SetShiftRotateOperationFlags(val);
		}

		private void SRLRegister(Instruction inst)
		{
			byte reg = (byte)(inst.Opcode & 0x07);
			byte val = Machine.CPU.ReadRegister(reg);
			Machine.CPU.CarryFlag = (val & 0x01) == 0x01;
			val = (byte)(val >> 1);
			Machine.CPU.WriteRegister(reg, val);
			SetShiftRotateOperationFlags(val);
		}

		private void SRLHLIndirect(Instruction inst)
		{
			ushort address = GetIndexedAddress(inst);
			byte val = Machine.Memory.Read(address);
			Machine.CPU.CarryFlag = (val & 0x01) == 0x01;
			val = (byte)(val >> 1);
			Machine.Memory.Write(address, val);
			SetShiftRotateOperationFlags(val);
		}

		private void BITRegister(Instruction inst)
		{
			byte reg = (byte)(inst.Opcode & 0x07);
			byte bit = (byte)((inst.Opcode & 0x38) >> 3);
			byte val = Machine.CPU.ReadRegister(reg);
			byte result = (byte)(val & _bitMap[bit]);
			Machine.CPU.ZeroFlag = result == 0x00;
			Machine.CPU.HalfCarryFlag = true;
			Machine.CPU.SubtractFlag = false;
		}

		private void BITHLIndirect(Instruction inst)
		{
			byte bit = (byte)((inst.Opcode & 0x38) >> 3);
			ushort address = GetIndexedAddress(inst);
			byte val = Machine.Memory.Read(address);
			byte result = (byte)(val & _bitMap[bit]);
			Machine.CPU.ZeroFlag = result == 0x00;
			Machine.CPU.HalfCarryFlag = true;
			Machine.CPU.SubtractFlag = false;
		}

		private void RESRegister(Instruction inst)
		{
			byte reg = (byte)(inst.Opcode & 0x07);
			byte bit = (byte)((inst.Opcode & 0x38) >> 3);
			byte val = Machine.CPU.ReadRegister(reg);
			val &= (byte)~_bitMap[bit];
			Machine.CPU.WriteRegister(reg, val);
		}

		private void RESHLIndirect(Instruction inst)
		{
			byte bit = (byte)((inst.Opcode & 0x38) >> 3);
			ushort address = GetIndexedAddress(inst);
			byte val = Machine.Memory.Read(address);
			val &= (byte)~_bitMap[bit];
			Machine.Memory.Write(address, val);
		}

		private void SETRegister(Instruction inst)
		{
			byte reg = (byte)(inst.Opcode & 0x07);
			byte bit = (byte)((inst.Opcode & 0x38) >> 3);
			byte val = Machine.CPU.ReadRegister(reg);
			val |= _bitMap[bit];
			Machine.CPU.WriteRegister(reg, val);
		}

		private void SETHLIndirect(Instruction inst)
		{
			byte bit = (byte)((inst.Opcode & 0x38) >> 3);
			ushort address = GetIndexedAddress(inst);
			byte val = Machine.Memory.Read(address);
			val |= _bitMap[bit];
			Machine.Memory.Write(address, val);
		}


		// ED prefixed opcodes

		private void INRegister(Instruction inst)
		{
			byte reg = (byte)((inst.Opcode & 0x38) >> 3);
			byte val = Machine.Port.Read(Machine.CPU.BC);
			Machine.CPU.WriteRegister(reg, val);

			Machine.CPU.SubtractFlag = false;
			Machine.CPU.HalfCarryFlag = false;
			Machine.CPU.ZeroFlag = val == 0;
			Machine.CPU.SignFlag = (val & 0x80) == 0x80;
			Machine.CPU.ParityOverflowFlag = Helpers.Parity(val);
		}

		private void OUTRegister(Instruction inst)
		{
			byte reg = (byte)((inst.Opcode & 0x38) >> 3);
			byte val = Machine.CPU.ReadRegister(reg);
			Machine.Port.Write(Machine.CPU.BC, val);
		}

		private void SBCHLRegisterPair(Instruction inst)
		{
			byte reg = (byte)((inst.Opcode & 0x30) >> 4);
			ushort val = Machine.CPU.ReadRegisterPair(reg);
			Machine.CPU.HL = Subtract16Bit(Machine.CPU.HL, val, true);
		}

		private void ADCHLRegisterPair(Instruction inst)
		{
			byte reg = (byte)((inst.Opcode & 0x30) >> 4);
			ushort val = Machine.CPU.ReadRegisterPair(reg);
			Machine.CPU.HL = Add16Bit(Machine.CPU.HL, val, true);
		}

		private void LDAddressRegisterPair(Instruction inst)
		{
			byte reg = (byte)((inst.Opcode & 0x30) >> 4);
			ushort val = Machine.CPU.ReadRegisterPair(reg);
			Machine.Memory.Write16bit(inst.WordOperand, val);
		}

		private void LDRegisterPairAddress(Instruction inst)
		{
			byte reg = (byte)((inst.Opcode & 0x30) >> 4);
			ushort val = Machine.Memory.Read16bit(inst.WordOperand);
			Machine.CPU.WriteRegisterPair(reg, val);
		}

		private void NEG(Instruction inst)
		{
			byte prevVal = Machine.CPU.A;
			Machine.CPU.A = (byte)(~prevVal + 1);

			Machine.CPU.SubtractFlag = true;
			Machine.CPU.ZeroFlag = Machine.CPU.A == 0;
			Machine.CPU.SignFlag = (Machine.CPU.A & 0x80) == 0x80;
			Machine.CPU.CarryFlag = prevVal != 0;
			Machine.CPU.HalfCarryFlag = ((prevVal & 0x0F) + (Machine.CPU.A & 0x0F)) > 0x0F;
			Machine.CPU.ParityOverflowFlag = prevVal == 0x80;
		}

		private void RETN(Instruction inst)
		{
			Machine.CPU.PC = Machine.Stack.Pop();
			Machine.CPU.IFF1 = Machine.CPU.IFF2;
		}

		private void InterruptMode0(Instruction inst)
		{
			Machine.CPU.InterruptMode = InterruptMode.Mode0;
		}

		private void LDIAcc(Instruction inst)
		{
			Machine.CPU.I = Machine.CPU.A;
		}

		private void RETI(Instruction inst)
		{
			Machine.CPU.PC = Machine.Stack.Pop();
		}

		private void LDRAcc(Instruction inst)
		{
			Machine.CPU.R = Machine.CPU.A;
		}

		private void InterruptMode1(Instruction inst)
		{
			Machine.CPU.InterruptMode = InterruptMode.Mode1;
		}

		private void LDAccI(Instruction inst)
		{
			Machine.CPU.A = Machine.CPU.I;
			Machine.CPU.ZeroFlag = Machine.CPU.A == 0;
			Machine.CPU.SignFlag = (Machine.CPU.A & 0x80) == 0x80;
			Machine.CPU.SubtractFlag = false;
			Machine.CPU.HalfCarryFlag = false;
			Machine.CPU.ParityOverflowFlag = Machine.CPU.IFF2;
		}

		private void InterruptMode2(Instruction inst)
		{
			Machine.CPU.InterruptMode = InterruptMode.Mode2;
		}

		private void LDAccR(Instruction inst)
		{
			Machine.CPU.A = Machine.CPU.R;
			Machine.CPU.ZeroFlag = Machine.CPU.A == 0;
			Machine.CPU.SignFlag = (Machine.CPU.A & 0x80) == 0x80;
			Machine.CPU.SubtractFlag = false;
			Machine.CPU.HalfCarryFlag = false;
			Machine.CPU.ParityOverflowFlag = Machine.CPU.IFF2;
		}

		private void RRD(Instruction inst)
		{
			byte memVal = Machine.Memory.Read(Machine.CPU.HL);
			byte loNibbleMem = (byte)(memVal & 0x0F);
			byte hiNibbleMem = (byte)(memVal & 0xF0);
			byte loNibbleAcc = (byte)(Machine.CPU.A & 0x0F);
			byte hiNibbleAcc = (byte)(Machine.CPU.A & 0xF0);

			Machine.CPU.A = (byte)(hiNibbleAcc + loNibbleMem);
			memVal = (byte)((loNibbleAcc << 4) + (hiNibbleMem >> 4));
			Machine.Memory.Write(Machine.CPU.HL, memVal);

			Machine.CPU.ZeroFlag = Machine.CPU.A == 0;
			Machine.CPU.SignFlag = (Machine.CPU.A & 0x80) == 0x80;
			Machine.CPU.SubtractFlag = false;
			Machine.CPU.HalfCarryFlag = false;
			Machine.CPU.ParityOverflowFlag = Helpers.Parity(Machine.CPU.A);
		}

		private void RLD(Instruction inst)
		{
			byte memVal = Machine.Memory.Read(Machine.CPU.HL);
			byte loNibbleMem = (byte)(memVal & 0x0F);
			byte hiNibbleMem = (byte)(memVal & 0xF0);
			byte loNibbleAcc = (byte)(Machine.CPU.A & 0x0F);
			byte hiNibbleAcc = (byte)(Machine.CPU.A & 0xF0);

			Machine.CPU.A = (byte)(hiNibbleAcc + (hiNibbleMem >> 4));
			memVal = (byte)((loNibbleMem << 4) + loNibbleAcc);
			Machine.Memory.Write(Machine.CPU.HL, memVal);

			Machine.CPU.ZeroFlag = Machine.CPU.A == 0;
			Machine.CPU.SignFlag = (Machine.CPU.A & 0x80) == 0x80;
			Machine.CPU.SubtractFlag = false;
			Machine.CPU.HalfCarryFlag = false;
			Machine.CPU.ParityOverflowFlag = Helpers.Parity(Machine.CPU.A);
		}

		private void LDIncDec(Instruction inst)
		{
			byte memVal = Machine.Memory.Read(Machine.CPU.HL);
			Machine.Memory.Write(Machine.CPU.DE, memVal);
			if ((inst.Opcode & 0x08) == 0x08)               // If bit 3 is set then we're executing an LDD instruction
			{
				Machine.CPU.HL--;
				Machine.CPU.DE--;
			}
			else
			{
				Machine.CPU.HL++;
				Machine.CPU.DE++;
			}
			Machine.CPU.BC--;
			Machine.CPU.SubtractFlag = false;
			Machine.CPU.HalfCarryFlag = false;
			Machine.CPU.ParityOverflowFlag = Machine.CPU.BC != 0;
		}

		private void CPIncDec(Instruction inst)
		{
			byte memVal = Machine.Memory.Read(Machine.CPU.HL);
			byte diff = (byte)(Machine.CPU.A - memVal);
			if ((inst.Opcode & 0x08) == 0x08)               // If bit 3 is set then we're executing a CPD instruction
			{
				Machine.CPU.HL--;
				Machine.CPU.DE--;
			}
			else
			{
				Machine.CPU.HL++;
				Machine.CPU.DE++;
			}
			Machine.CPU.BC--;
			Machine.CPU.ZeroFlag = diff == 0;
			Machine.CPU.SignFlag = (diff & 0x80) == 0x80;
			Machine.CPU.SubtractFlag = true;
			Machine.CPU.HalfCarryFlag = (Machine.CPU.A & 0x0F) < (memVal & 0x0F);
			Machine.CPU.ParityOverflowFlag = Machine.CPU.BC != 0;
		}

		private void INIncDec(Instruction inst)
		{
			byte val = Machine.Port.Read(Machine.CPU.BC);
			Machine.Memory.Write(Machine.CPU.HL, val);
			if ((inst.Opcode & 0x08) == 0x08)               // If bit 3 is set then we're executing an IND instruction
			{
				Machine.CPU.HL--;
			}
			else
			{
				Machine.CPU.HL++;
			}
			Machine.CPU.B--;
			Machine.CPU.ZeroFlag = Machine.CPU.B == 0;
			Machine.CPU.SubtractFlag = true;
		}

		private void OUTIncDec(Instruction inst)
		{
			byte memVal = Machine.Memory.Read(Machine.CPU.HL);
			Machine.Port.Write(Machine.CPU.BC, memVal);
			if ((inst.Opcode & 0x08) == 0x08)               // If bit 3 is set then we're executing an OUTD instruction
			{
				Machine.CPU.HL--;
			}
			else
			{
				Machine.CPU.HL++;
			}
			Machine.CPU.B--;
			Machine.CPU.ZeroFlag = Machine.CPU.B == 0;
			Machine.CPU.SubtractFlag = true;
		}

		private void LDIncDecRepeat(Instruction inst)
		{
			do
			{
				byte memVal = Machine.Memory.Read(Machine.CPU.HL);
				Machine.Memory.Write(Machine.CPU.DE, memVal);
				if ((inst.Opcode & 0x08) == 0x08)               // If bit 3 is set then we're executing an LDDR instruction
				{
					Machine.CPU.HL--;
					Machine.CPU.DE--;
				}
				else
				{
					Machine.CPU.HL++;
					Machine.CPU.DE++;
				}
				Machine.CPU.BC--;
			} while (Machine.CPU.BC != 0);
			Machine.CPU.SubtractFlag = false;
			Machine.CPU.HalfCarryFlag = false;
			Machine.CPU.ParityOverflowFlag = false;
		}

		private void CPIncDecRepeat(Instruction inst)
		{
			byte diff;
			byte memVal;
			do
			{
				memVal = Machine.Memory.Read(Machine.CPU.HL);
				diff = (byte)(Machine.CPU.A - memVal);
				if ((inst.Opcode & 0x08) == 0x08)               // If bit 3 is set then we're executing a CPDR instruction
				{
					Machine.CPU.HL--;
					Machine.CPU.DE--;
				}
				else
				{
					Machine.CPU.HL++;
					Machine.CPU.DE++;
				}
				Machine.CPU.BC--;
			} while (Machine.CPU.BC != 0 && diff != 0);
			Machine.CPU.ZeroFlag = diff == 0;
			Machine.CPU.SignFlag = (diff & 0x80) == 0x80;
			Machine.CPU.SubtractFlag = true;
			Machine.CPU.HalfCarryFlag = (Machine.CPU.A & 0x0F) < (memVal & 0x0F);
			Machine.CPU.ParityOverflowFlag = Machine.CPU.BC != 0;
		}

		private void INIncDecRepeat(Instruction inst)
		{
			do
			{
				byte val = Machine.Port.Read(Machine.CPU.BC);
				Machine.Memory.Write(Machine.CPU.HL, val);
				if ((inst.Opcode & 0x08) == 0x08)               // If bit 3 is set then we're executing an INDR instruction
				{
					Machine.CPU.HL--;
				}
				else
				{
					Machine.CPU.HL++;
				}
				Machine.CPU.B--;
			} while (Machine.CPU.B != 0);
			Machine.CPU.ZeroFlag = true;
			Machine.CPU.SubtractFlag = true;
		}

		private void OUTIncDecRepeat(Instruction inst)
		{
			do
			{
				byte memVal = Machine.Memory.Read(Machine.CPU.HL);
				Machine.Port.Write(Machine.CPU.BC, memVal);
				if ((inst.Opcode & 0x08) == 0x08)               // If bit 3 is set then we're executing an OTDR instruction
				{
					Machine.CPU.HL--;
				}
				else
				{
					Machine.CPU.HL++;
				}
				Machine.CPU.B--;
			} while (Machine.CPU.B != 0);
			Machine.CPU.ZeroFlag = true;
			Machine.CPU.SubtractFlag = true;
		}

		// DD Prefixed instructions

		private void ADDIXRegisterPair(Instruction inst)
		{
			var regPair = (byte)((inst.Opcode & 0x30) >> 4);
			var regValue = regPair == 2 ? Machine.CPU.IX : Machine.CPU.ReadRegisterPair(regPair);
			Machine.CPU.IX = Add16Bit(Machine.CPU.IX, regValue, false);
		}

		private void LDIXAddress(Instruction inst)
		{
			Machine.CPU.IX = inst.WordOperand;
		}

		private void LDAddressIndirectIX(Instruction inst)
		{
			Machine.Memory.Write16bit(inst.WordOperand, Machine.CPU.IX);
		}

		private void INCIX(Instruction inst)
		{
			Machine.CPU.IX++;
		}

		private void LDIXAddressIndirect(Instruction inst)
		{
			Machine.CPU.IX = Machine.Memory.Read16bit(inst.WordOperand);
		}

		private void DECIX(Instruction inst)
		{
			Machine.CPU.IX--;
		}

		private void POPIX(Instruction inst)
		{
			Machine.CPU.IX = Machine.Stack.Pop();
		}

		private void EXSPIX(Instruction inst)
		{
			ushort value = Machine.Stack.Pop();
			Machine.Stack.Push(Machine.CPU.IX);
			Machine.CPU.IX = value;
		}

		private void PUSHIX(Instruction inst)
		{
			Machine.Stack.Push(Machine.CPU.IX);
		}

		private void JPIX(Instruction inst)
		{
			Machine.CPU.PC = Machine.CPU.IX;
		}

		private void LDSPIX(Instruction inst)
		{
			Machine.CPU.SP = Machine.CPU.IX;
		}


		// FD Prefixed instructions

		private void ADDIYRegisterPair(Instruction inst)
		{
			var regPair = (byte)((inst.Opcode & 0x30) >> 4);
			var regValue = regPair == 2 ? Machine.CPU.IY : Machine.CPU.ReadRegisterPair(regPair);
			Machine.CPU.IY = Add16Bit(Machine.CPU.IY, regValue, false);
		}

		private void LDIYAddress(Instruction inst)
		{
			Machine.CPU.IY = inst.WordOperand;
		}

		private void LDAddressIndirectIY(Instruction inst)
		{
			Machine.Memory.Write16bit(inst.WordOperand, Machine.CPU.IY);
		}

		private void INCIY(Instruction inst)
		{
			Machine.CPU.IY++;
		}

		private void LDIYAddressIndirect(Instruction inst)
		{
			Machine.CPU.IY = Machine.Memory.Read16bit(inst.WordOperand);
		}

		private void DECIY(Instruction inst)
		{
			Machine.CPU.IY--;
		}

		private void POPIY(Instruction inst)
		{
			Machine.CPU.IY = Machine.Stack.Pop();
		}

		private void EXSPIY(Instruction inst)
		{
			ushort value = Machine.Stack.Pop();
			Machine.Stack.Push(Machine.CPU.IY);
			Machine.CPU.IY = value;
		}

		private void PUSHIY(Instruction inst)
		{
			Machine.Stack.Push(Machine.CPU.IY);
		}

		private void JPIY(Instruction inst)
		{
			Machine.CPU.PC = Machine.CPU.IY;
		}

		private void LDSPIY(Instruction inst)
		{
			Machine.CPU.SP = Machine.CPU.IY;
		}

	}
}
