namespace PendleCodeMonkey.Z80EmulatorLib.Assembler
{
	/// <summary>
	/// Implementation of the <see cref="AssemblerEnumerations"/> class.
	/// </summary>
	public class AssemblerEnumerations
	{
		/// <summary>
		/// Enumeration of operand types - used during assembler operation.
		/// </summary>
		internal enum OperandType
		{
			None,
			Implied,
			Register,
			RegisterPair,
			RegisterIndirect,
			RegisterPairIndirect,
			Indexed,
			Relative,
			Flag,
			Immediate,
			Indirect,
			Unresolved,
			UnresolvedIndirect
		}

		/// <summary>
		/// Enumeration of data segment types.
		/// </summary>
		/// <remarks>
		/// These correspond to the DB, DW, and DS assembler directives; Byte for DB, Word for DW, and Space for DS.
		/// </remarks>
		public enum DataSegmentType
		{
			None,
			Byte,
			Word,
			Space
		}

		/// <summary>
		/// Enumeration of errors that can occur during assembler operation.
		/// </summary>
		public enum Errors
		{
			None,
			CannotHaveDuplicateLabelNames,
			OrgAddressOutOfValidRange,
			InvalidOrgAddress,
			InvalidInstruction,
			UnresolvedOperandValue,
			CannotRedefineEquValue,
			CurrentAddressOutOfRange,
			InvalidByteSegmentValue,
			ByteSegmentValueOutOfRange,
			InvalidWordSegmentValue,
			WordSegmentValueOutOfRange,
			SpaceSegmentSizeOutOfRange,
			SpaceSegmentInitializeValueOutOfRange,
			SpaceSegmentInvalidParameter,
			OperandValueOutOfRange,
			DisplacementOutOfRange,
			DivideByZero,
			EQUNameCannotBeReservedWord,
			LabelNameCannotBeReservedWord
		}
	}
}
