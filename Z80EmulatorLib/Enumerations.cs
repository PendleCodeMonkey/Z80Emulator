using System;

namespace PendleCodeMonkey.Z80EmulatorLib.Enumerations
{

	/// <summary>
	/// Enumeration of the processor flags.
	/// </summary>
	[Flags]
	public enum ProcessorFlags : byte
	{
		Carry = 0x01,
		Subtract = 0x02,
		ParityOverflow = 0x04,
		HalfCarry = 0x10,
		Zero = 0x40,
		Sign = 0x80
	};

	/// <summary>
	/// Enumeration of the Z80 processor's 10 addressing modes.
	/// </summary>
	public enum AddrMode : byte
	{
		Implied,
		Immediate,
		ExtendedImmediate,
		Register,
		RegisterIndirect,
		Extended,
		ModifiedPageZero,
		Relative,
		Indexed,
		Bit
	};

	/// <summary>
	/// Enumeration of the Z80 processor's 3 interrupt modes.
	/// </summary>
	public enum InterruptMode : byte
	{
		Mode0,
		Mode1,
		Mode2
	}

	/// <summary>
	/// Enumeration of opcode prefix values. For example, None for unprefixed instructions, CB for instructions
	/// that have a 0xCB prefix byte, DDCB for instructions that have both 0xDD 0xCB prefix bytes, etc.
	/// </summary>
	public enum OpcodePrefix : byte
	{
		None,
		CB,
		ED,
		DD,
		FD,
		DDCB,
		FDCB
	};

	/// <summary>
	/// Enumeration of Operation Handler identifiers.
	/// </summary>
	public enum OpHandlerID : byte
	{
		NONE,

		// Unprefixed opcodes
		NOP,
		LDRegisterPair,     // Load register pair - BC, DE, HL, SP
		LDRegisterPairAcc,     // LD (BC),A ; LD (DE),A ; LD (HL),A ; LD (nn),A
		LDAddressAcc,     // LD (nn),A
		LDRegister,     // Load Register to Register, Immediate to Register, etc.
		LDRegisterHL,   // Load (HL) to Register.
		LDHLRegister,   // Load Register to (HL).
		ADDAccRegister,   // Add Register to A.
		ADDAccHL,       // Add (HL) to A.
		ADCAccRegister,   // ADC Register to A.
		ADCAccHL,       // ADC (HL) to A.
		SUBAccRegister,   // SUB Register from A.
		SUBAccHL,       // SUB (HL) from A.
		SBCAccRegister,   // SBC Register from A.
		SBCAccHL,       // SBC (HL) from A.
		ANDRegister,     // AND Register with A, Immediate with A, etc.
		ANDHL,          // AND (HL) with A.
		XORRegister,     // XOR Register with A, Immediate with A, etc.
		XORHL,          // XOR (HL) with A.
		ORRegister,     // OR Register with A, Immediate with A, etc.
		ORHL,          // OR (HL) with A.
		CPRegister,     // CP Register with A, Immediate with A, etc.
		CPHL,          // CP (HL) with A.
		INCRegister,     // Increment Register.
		DECRegister,     // Decrement Register.
		INCRegisterPair,     // Increment Register Pair.
		DECRegisterPair,     // Decrement Register Pair.
		HALT,                // HALT
		RETCondition,        // Conditional RET
		JPCondition,        // Conditional JP
		CALLCondition,      // Conditional CALL
		RST,                // RST
		POPRegisterPair,    // POP BC, DE, HL, or AF
		JP,                 // Unconditional JP
		PUSHRegisterPair,   // PUSH BC, DE, HL, or AF
		RET,                // Unconditional RET
		CALL,                // Unconditional CALL
		OUTAcc,              // OUT (n),A
		INAcc,               // IN A,(n)
		EXX,               // EXX
		EXSPHL,            // EX (SP),HL
		JPHL,               // JP (HL)
		EXDEHL,            // EX DE,HL
		LDSPHL,            // LD SP,HL
		DI,            // DI
		EI,            // EI
		RLCA,            // RLCA
		RRCA,            // RRCA
		EXAF,            // RRCA
		LDAccRegisterPair,     // LD A,(BC) ; LD A,(DE)
		LDAccAddress,     // LD A,(nn)
		ADDHLRegisterPair,     // ADD HL,BC ; ADD HL,DE ; ADD HL,HL ; ADD HL,SP
		DJNZ,            // DJNZ e
		RLA,            // RLA
		JR,            // JR e
		RRA,            // RRA
		JRCondition,            // JR NZ ; JR Z ; JR NC ; JR C
		LDAddressHL,            // LD (nn),HL
		LDHLAddress,            // LD HL,(nn)
		DAA,            // DAA
		CPL,            // CPL
		CCF,            // CCF
		SCF,            // SCF
		INCHLIndirect,            // INC (HL)
		DECHLIndirect,            // DEC (HL)
		LDHLIndirectImm,          // LD (HL),n

		// CB prefixed opcodes
		RLCRegister,		// RLC B, RLC C, etc.
		RLCHLIndirect,		// RLC (HL)
		RRCRegister,        // RRC B, RRC C, etc.
		RRCHLIndirect,      // RRC (HL)
		RLRegister,        // RL B, RL C, etc.
		RLHLIndirect,      // RL (HL)
		RRRegister,        // RR B, RR C, etc.
		RRHLIndirect,      // RR (HL)
		SLARegister,        // SLA B, SLA C, etc.
		SLAHLIndirect,      // SLA (HL)
		SRARegister,        // SRA B, SRA C, etc.
		SRAHLIndirect,      // SRA (HL)
		SRLRegister,        // SRL B, SRL C, etc.
		SRLHLIndirect,      // SRL (HL)
		BITRegister,        // BIT n,B ; BIT n,C ; etc.
		BITHLIndirect,      // BIT n,(HL)
		RESRegister,        // RES n,B ; RES n,C ; etc.
		RESHLIndirect,      // RES n,(HL)
		SETRegister,        // SET n,B ; SET n,C ; etc.
		SETHLIndirect,      // SET n,(HL)

		// ED prefixed opcodes
		INRegister,        // IN B,(C) ; IN C,(C) ; etc.
		OUTRegister,        // OUT B,(C) ; OUT C,(C) ; etc.
		SBCHLRegisterPair,    // SBC HL,BC ; SBC HL,DE ; SBC HL,HL ; SBC HL,SP
		ADCHLRegisterPair,    // ADC HL,BC ; ADC HL,DE ; ADC HL,HL ; ADC HL,SP
		LDAddressRegisterPair,    // LD (nn),BC ; LD (nn),DE ; LD (nn),SP
		LDRegisterPairAddress,    // LD BC,(nn) ; LD DE,(nn) ; LD SP,(nn)
		NEG,                    // NEG
		RETN,                    // RETN
		InterruptMode0,          // IM 0
		LDIAcc,                 // LD I,A
		RETI,                    // RETI
		LDRAcc,                 // LD R,A
		InterruptMode1,          // IM 1
		LDAccI,                 // LD A,I
		InterruptMode2,          // IM 2
		LDAccR,                 // LD A,R
		RRD,                  // RRD
		RLD,                  // RLD

		LDIncDec,			  // LDI, LDD
		CPIncDec,			  // CPI, CPD
		INIncDec,			  // INI, IND
		OUTIncDec,			  // OUTI, OUTD

		LDIncDecRepeat,       // LDIR, LDDR
		CPIncDecRepeat,       // CPIR, CPDR
		INIncDecRepeat,       // INIR, INDR
		OUTIncDecRepeat,      // OTIR, OTDR

		// Special value used for DD, FD, DDCB, and FDCB prefixed instructions.
		IXIYIndirect,           // When IXIYIndirect is specified, the handler is inferred from the instruction info
								// e.g. (IX+d) instructions will use corresponding (HL) methods, replacing HL with IX+d, etc.

		// DD prefixed opcodes
		ADDIXRegisterPair,       // ADD IX,BC ; ADD IX,DE ; ADD IX,IX ; ADD IX,SP
		LDIXAddress,             // LD IX,nn
		LDAddressIndirectIX,     // LD (nn),IX
		INCIX,                   // INC IX
		LDIXAddressIndirect,     // LD IX,(nn)
		DECIX,                   // DEC IX
		POPIX,                   // POP IX
		EXSPIX,                  // EX (SP),IX
		PUSHIX,                  // PUSH IX
		JPIX,                    // JP (IX)
		LDSPIX,                  // LD SP,IX

		// FD prefixed opcodes
		ADDIYRegisterPair,       // ADD IY,BC ; ADD IY,DE ; ADD IY,IY ; ADD IY,SP
		LDIYAddress,             // LD IY,nn
		LDAddressIndirectIY,     // LD (nn),IY
		INCIY,                   // INC IY
		LDIYAddressIndirect,     // LD IY,(nn)
		DECIY,                   // DEC IY
		POPIY,                   // POP IY
		EXSPIY,                  // EX (SP),IY
		PUSHIY,                  // PUSH IY
		JPIY,                    // JP (IY)
		LDSPIY,                  // LD SP,IY
	};

}
