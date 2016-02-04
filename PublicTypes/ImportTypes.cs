using System;
using System.Collections.Generic;
using System.Reflection;

// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public License
// as published by the Free Software Foundation; either version 2.1
// of the License, or (at your option) any later version.
// 
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free
// Software Foundation, Inc., 59 Temple Place, Suite 330, Boston,
// MA 02111-1307, USA.
// 
// Flee# - A port of Eugene Ciloci's Flee to C#
// Copyright © 2012 Yoni Gozman
//

namespace Ciloci.Flee
{

	/// <include file='Resources/DocComments.xml' path='DocComments/ImportBase/Class/*' />
	public abstract class ImportBase : IEnumerable<ImportBase>, IEquatable<ImportBase>
	{


		private ExpressionContext MyContext;

		internal ImportBase()
		{
		}

		#region "Methods - Non Public"
		internal virtual void SetContext(ExpressionContext context)
		{
			MyContext = context;
			this.Validate();
		}

		internal abstract void Validate();

		protected abstract void AddMembers(string memberName, MemberTypes memberType, ICollection<MemberInfo> dest);
		protected abstract void AddMembers(MemberTypes memberType, ICollection<MemberInfo> dest);

		internal ImportBase Clone()
		{
            return this.MemberwiseClone() as ImportBase;
		}

		protected static void AddImportMembers(ImportBase import, string memberName, MemberTypes memberType, ICollection<MemberInfo> dest)
		{
			import.AddMembers(memberName, memberType, dest);
		}

		protected static void AddImportMembers(ImportBase import, MemberTypes memberType, ICollection<MemberInfo> dest)
		{
			import.AddMembers(memberType, dest);
		}

		protected static void AddMemberRange(ICollection<MemberInfo> members, ICollection<MemberInfo> dest)
		{
			foreach (MemberInfo mi in members) {
				dest.Add(mi);
			}
		}

		protected bool AlwaysMemberFilter(MemberInfo member, object criteria)
		{
			return true;
		}

		internal abstract bool IsMatch(string name);
		internal abstract Type FindType(string typename);

		internal virtual ImportBase FindImport(string name)
		{
			return null;
		}

		internal MemberInfo[] FindMembers(string memberName, MemberTypes memberType)
		{
			List<MemberInfo> found = new List<MemberInfo>();
			this.AddMembers(memberName, memberType, found);
			return found.ToArray();
		}
		#endregion

		#region "Methods - Public"
		public MemberInfo[] GetMembers(MemberTypes memberType)
		{
			List<MemberInfo> found = new List<MemberInfo>();
			this.AddMembers(memberType, found);
			return found.ToArray();
		}
		#endregion

		#region "IEnumerable Implementation"
		public virtual System.Collections.Generic.IEnumerator<ImportBase> GetEnumerator()
		{
			List<ImportBase> coll = new List<ImportBase>();
			return coll.GetEnumerator();
		}

		private System.Collections.IEnumerator GetEnumerator1()
		{
			return this.GetEnumerator();
		}
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator1();
		}
		#endregion

		#region "IEquatable Implementation"
		public bool Equals(ImportBase other)
		{
			return this.EqualsInternal(other);
		}

		protected abstract bool EqualsInternal(ImportBase import);
		#endregion

		#region "Properties - Protected"
		protected ExpressionContext Context {
			get { return MyContext; }
		}
		#endregion

		#region "Properties - Public"
		/// <include file='Resources/DocComments.xml' path='DocComments/ImportBase/Name/*' />
		public abstract string Name { get; }

		/// <include file='Resources/DocComments.xml' path='DocComments/ImportBase/IsContainer/*' />
		public virtual bool IsContainer {
			get { return false; }
		}
		#endregion
	}
}
namespace Ciloci.Flee
{

	/// <include file='Resources/DocComments.xml' path='DocComments/TypeImport/Class/*' />
	public sealed class TypeImport : ImportBase
	{

		private Type MyType;
		private BindingFlags MyBindFlags;

		private bool MyUseTypeNameAsNamespace;
		/// <include file='Resources/DocComments.xml' path='DocComments/TypeImport/New1/*' />
		public TypeImport(Type importType) : this(importType, false)
		{
		}

		/// <include file='Resources/DocComments.xml' path='DocComments/TypeImport/New2/*' />
		public TypeImport(Type importType, bool useTypeNameAsNamespace) : this(importType, BindingFlags.Public | BindingFlags.Static, useTypeNameAsNamespace)
		{
		}

		#region "Methods - Non Public"
		internal TypeImport(Type t, BindingFlags flags, bool useTypeNameAsNamespace)
		{
			Utility.AssertNotNull(t, "t");
			MyType = t;
			MyBindFlags = flags;
			MyUseTypeNameAsNamespace = useTypeNameAsNamespace;
		}

		internal override void Validate()
		{
			this.Context.AssertTypeIsAccessible(MyType);
		}

		protected override void AddMembers(string memberName, MemberTypes memberType, ICollection<MemberInfo> dest)
		{
			MemberInfo[] members = MyType.FindMembers(memberType, MyBindFlags, this.Context.Options.MemberFilter, memberName);
			ImportBase.AddMemberRange(members, dest);
		}

		protected override void AddMembers(MemberTypes memberType, ICollection<MemberInfo> dest)
		{
			if (MyUseTypeNameAsNamespace == false) {
				MemberInfo[] members = MyType.FindMembers(memberType, MyBindFlags, this.AlwaysMemberFilter, null);
				ImportBase.AddMemberRange(members, dest);
			}
		}

		internal override bool IsMatch(string name)
		{
			if (MyUseTypeNameAsNamespace == true) {
				return string.Equals(MyType.Name, name, this.Context.Options.MemberStringComparison);
			} else {
				return false;
			}
		}

		internal override Type FindType(string typeName)
		{
			if (string.Equals(typeName, MyType.Name, this.Context.Options.MemberStringComparison) == true) {
				return MyType;
			} else {
				return null;
			}
		}

		protected override bool EqualsInternal(ImportBase import)
		{
			TypeImport otherSameType = import as TypeImport;
			return (otherSameType != null) && object.ReferenceEquals(MyType, otherSameType.MyType);
		}
		#endregion

		#region "Methods - Public"
		public override IEnumerator<ImportBase> GetEnumerator()
		{
			if (MyUseTypeNameAsNamespace == true) {
				List<ImportBase> coll = new List<ImportBase>();
				coll.Add(new TypeImport(MyType, false));
				return coll.GetEnumerator();
			} else {
				return base.GetEnumerator();
			}
		}
		#endregion

		#region "Properties - Public"
		public override bool IsContainer {
			get { return MyUseTypeNameAsNamespace; }
		}

		public override string Name {
			get { return MyType.Name; }
		}

		/// <include file='Resources/DocComments.xml' path='DocComments/TypeImport/Target/*' />
		public Type Target {
			get { return MyType; }
		}
		#endregion
	}
}
namespace Ciloci.Flee
{

	/// <include file='Resources/DocComments.xml' path='DocComments/MethodImport/Class/*' />
	public sealed class MethodImport : ImportBase
	{


		private MethodInfo MyMethod;
		/// <include file='Resources/DocComments.xml' path='DocComments/MethodImport/New/*' />
		public MethodImport(MethodInfo importMethod)
		{
			Utility.AssertNotNull(importMethod, "importMethod");
			MyMethod = importMethod;
		}

		internal override void Validate()
		{
			this.Context.AssertTypeIsAccessible(MyMethod.ReflectedType);
		}

		protected override void AddMembers(string memberName, MemberTypes memberType, ICollection<MemberInfo> dest)
		{
			if (string.Equals(memberName, MyMethod.Name, this.Context.Options.MemberStringComparison) == true && (memberType & MemberTypes.Method) != 0) {
				dest.Add(MyMethod);
			}
		}

		protected override void AddMembers(MemberTypes memberType, ICollection<MemberInfo> dest)
		{
			if ((memberType & MemberTypes.Method) != 0) {
				dest.Add(MyMethod);
			}
		}

		internal override bool IsMatch(string name)
		{
			return string.Equals(MyMethod.Name, name, this.Context.Options.MemberStringComparison);
		}

		internal override Type FindType(string typeName)
		{
			return null;
		}

		protected override bool EqualsInternal(ImportBase import)
		{
			MethodImport otherSameType = import as MethodImport;
			return (otherSameType != null) && MyMethod.MethodHandle.Equals(otherSameType.MyMethod.MethodHandle);
		}

		public override string Name {
			get { return MyMethod.Name; }
		}

		/// <include file='Resources/DocComments.xml' path='DocComments/MethodImport/Target/*' />
		public MethodInfo Target {
			get { return MyMethod; }
		}
	}
}
namespace Ciloci.Flee
{

	/// <include file='Resources/DocComments.xml' path='DocComments/NamespaceImport/Class/*' />
	public sealed class NamespaceImport : ImportBase, ICollection<ImportBase>
	{

		private string MyNamespace;

		private List<ImportBase> MyImports;
		/// <include file='Resources/DocComments.xml' path='DocComments/NamespaceImport/New/*' />
		public NamespaceImport(string importNamespace)
		{
			Utility.AssertNotNull(importNamespace, "importNamespace");
			if (importNamespace.Length == 0) {
				string msg = Utility.GetGeneralErrorMessage(GeneralErrorResourceKeys.InvalidNamespaceName);
				throw new ArgumentException(msg);
			}

			MyNamespace = importNamespace;
			MyImports = new List<ImportBase>();
		}

		internal override void SetContext(ExpressionContext context)
		{
			base.SetContext(context);

			foreach (ImportBase import in MyImports) {
				import.SetContext(context);
			}
		}

		internal override void Validate()
		{
		}

		protected override void AddMembers(string memberName, MemberTypes memberType, ICollection<MemberInfo> dest)
		{
			foreach (ImportBase import in this.NonContainerImports) {
				AddImportMembers(import, memberName, memberType, dest);
			}
		}


		protected override void AddMembers(MemberTypes memberType, ICollection<MemberInfo> dest)
		{
		}

		internal override Type FindType(string typeName)
		{
			foreach (ImportBase import in this.NonContainerImports) {
				Type t = import.FindType(typeName);

				if ((t != null)) {
					return t;
				}
			}

			return null;
		}

		internal override ImportBase FindImport(string name)
		{
			foreach (ImportBase import in MyImports) {
				if (import.IsMatch(name) == true) {
					return import;
				}
			}
			return null;
		}

		internal override bool IsMatch(string name)
		{
			return string.Equals(MyNamespace, name, this.Context.Options.MemberStringComparison);
		}

		private ICollection<ImportBase> NonContainerImports {
			get {
				List<ImportBase> found = new List<ImportBase>();

				foreach (ImportBase import in MyImports) {
					if (import.IsContainer == false) {
						found.Add(import);
					}
				}

				return found;
			}
		}

		protected override bool EqualsInternal(ImportBase import)
		{
			NamespaceImport otherSameType = import as NamespaceImport;
			return (otherSameType != null) && MyNamespace.Equals(otherSameType.MyNamespace, this.Context.Options.MemberStringComparison);
		}

		public override bool IsContainer {
			get { return true; }
		}

		public override string Name {
			get { return MyNamespace; }
		}

		#region "ICollection implementation"

		public void Add(ImportBase item)
		{
			Utility.AssertNotNull(item, "item");

			if ((this.Context != null)) {
				item.SetContext(this.Context);
			}

			MyImports.Add(item);
		}

		public void Clear()
		{
			MyImports.Clear();
		}

		public bool Contains(ImportBase item)
		{
			return MyImports.Contains(item);
		}

		public void CopyTo(ImportBase[] array, int arrayIndex)
		{
			MyImports.CopyTo(array, arrayIndex);
		}

		public bool Remove(ImportBase item)
		{
			return MyImports.Remove(item);
		}

		public override IEnumerator<ImportBase> GetEnumerator()
		{
			return MyImports.GetEnumerator();
		}

		public int Count {
			get { return MyImports.Count; }
		}

        public bool IsReadOnly
        {
			get { return false; }
		}

		#endregion
	}
}
