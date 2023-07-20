using Mono.Cecil;
using System;

namespace Reflector.UI
{
	internal struct AssemblyPair
	{
		public AssemblyDefinition Assembly;

		public MemberReference Member;
	}
}