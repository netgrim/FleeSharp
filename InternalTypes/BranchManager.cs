
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Emit;

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

	/// <summary>
	/// Manages branch information and allows us to determine if we should emit a short or long branch
	/// </summary>
	internal class BranchManager
	{

		private IList<BranchInfo> MyBranchInfos;
		private IDictionary<object, Label> MyKeyLabelMap;

		public BranchManager()
		{
			MyBranchInfos = new List<BranchInfo>();
			MyKeyLabelMap = new Dictionary<object, Label>();
		}

		/// <summary>
		/// Determine whether to use short or long branches
		/// </summary>
		/// <remarks></remarks>
		public void ComputeBranches()
		{
			var betweenBranches = new List<BranchInfo>();

			foreach (BranchInfo bi in MyBranchInfos) {
				betweenBranches.Clear();

				// Find any branches between the start and end locations of this branch
				this.FindBetweenBranches(bi, betweenBranches);

				// Count the number of long branches in the above set
				int longBranchesBetween = this.CountLongBranches(betweenBranches);

				// Adjust the branch as necessary
				bi.AdjustForLongBranchesBetween(longBranchesBetween);
			}

			int longBranchCount = 0;

			// Adjust the start location of each branch
			foreach (var bi in MyBranchInfos) {
				// Save the short/long branch type
				bi.BakeIsLongBranch();

				// Adjust the start location as necessary
				bi.AdjustForLongBranches(longBranchCount);

				// Keep a tally of the number of long branches
				longBranchCount += Convert.ToInt32(bi.IsLongBranch);
			}
		}

		/// <summary>
		/// Count the number of long branches in a set
		/// </summary>
		/// <param name="dest"></param>
		/// <returns></returns>
		/// <remarks></remarks>
		private int CountLongBranches(ICollection<BranchInfo> dest)
		{
			int count = 0;

			foreach (var bi in dest) {
				count += Convert.ToInt32(bi.ComputeIsLongBranch());
			}

			return count;
		}

		/// <summary>
		/// Find all the branches between the start and end locations of a target branch
		/// </summary>
		/// <param name="target"></param>
		/// <param name="dest"></param>
		/// <remarks></remarks>
		private void FindBetweenBranches(BranchInfo target, ICollection<BranchInfo> dest)
		{
			foreach (var bi in MyBranchInfos) {
				if (bi.IsBetween(target) == true) {
					dest.Add(bi);
				}
			}
		}

		/// <summary>
		/// Determine if a branch from a point to a label will be long
		/// </summary>
		/// <param name="ilg"></param>
		/// <param name="target"></param>
		/// <returns></returns>
		/// <remarks></remarks>
		public bool IsLongBranch(FleeILGenerator ilg, Label target)
		{
			var startLoc = new ILLocation(ilg.Length);
			var bi = new BranchInfo(startLoc, target);

			int index = MyBranchInfos.IndexOf(bi);

            //If it cannot be found, return the new branch
            if (index == -1)
                return bi.IsLongBranch;

			bi = MyBranchInfos[index];

			return bi.IsLongBranch;
		}

		/// <summary>
		/// Add a branch from a location to a target label
		/// </summary>
		/// <param name="ilg"></param>
		/// <param name="target"></param>
		/// <remarks></remarks>
		public void AddBranch(FleeILGenerator ilg, Label target)
		{
			var startLoc = new ILLocation(ilg.Length);

			var bi = new BranchInfo(startLoc, target);
			MyBranchInfos.Add(bi);
		}

		/// <summary>
		/// Get a label by a key
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		/// <remarks></remarks>
		public Label FindLabel(object key)
		{
			return MyKeyLabelMap[key];
		}

		/// <summary>
		/// Get a label by a key.  Create the label if it is not present.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="ilg"></param>
		/// <returns></returns>
		/// <remarks></remarks>
		public Label GetLabel(object key, FleeILGenerator ilg)
		{
			Label lbl;

			if (MyKeyLabelMap.TryGetValue(key, out lbl) == false) {
				lbl = ilg.DefineLabel();
				MyKeyLabelMap.Add(key, lbl);
			}

			return lbl;
		}

		/// <summary>
		/// Determines if we have a label for a key
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		/// <remarks></remarks>
		public bool HasLabel(object key)
		{
			return MyKeyLabelMap.ContainsKey(key);
		}

		/// <summary>
		/// Set the position for a label
		/// </summary>
		/// <param name="ilg"></param>
		/// <param name="target"></param>
		/// <remarks></remarks>
		public void MarkLabel(FleeILGenerator ilg, Label target)
		{
			int pos = ilg.Length;

			foreach (var bi in MyBranchInfos) {
				bi.Mark(target, pos);
			}
		}

		public override string ToString()
		{
			string[] arr = new string[MyBranchInfos.Count];

			for (int i = 0; i <= MyBranchInfos.Count - 1; i++) {
				arr[i] = MyBranchInfos[i].ToString();
			}

			return string.Join(System.Environment.NewLine, arr);
		}
	}
}
namespace Ciloci.Flee
{

	/// <summary>
	/// Represents a location in an IL stream
	/// </summary>
	internal class ILLocation : IEquatable<ILLocation>, IComparable<ILLocation>
	{


		private int MyPosition;
		/// <summary>
		/// ' Long branch is 5 bytes; short branch is 2; so we adjust by the difference
		/// </summary>

		private const int LongBranchAdjust = 3;
		/// <summary>
		/// Length of the Br_s opcode
		/// </summary>

		private const int Br_s_Length = 2;

		public ILLocation()
		{
		}

		public ILLocation(int position)
		{
			MyPosition = position;
		}

		public void SetPosition(int position)
		{
			MyPosition = position;
		}

		/// <summary>
		/// Adjust our position by a certain amount of long branches
		/// </summary>
		/// <param name="longBranchCount"></param>
		/// <remarks></remarks>
		public void AdjustForLongBranch(int longBranchCount)
		{
			MyPosition += longBranchCount * LongBranchAdjust;
		}

		/// <summary>
		/// Determine if this branch is long
		/// </summary>
		/// <param name="target"></param>
		/// <returns></returns>
		/// <remarks></remarks>
		public bool IsLongBranch(ILLocation target)
		{
			// The branch offset is relative to the instruction *after* the branch so we add 2 (length of a br_s) to our position
			return Utility.IsLongBranch(MyPosition + Br_s_Length, target.MyPosition);
		}

		public bool Equals1(ILLocation other)
		{
			return MyPosition == other.MyPosition;
		}
		bool System.IEquatable<ILLocation>.Equals(ILLocation other)
		{
			return Equals1(other);
		}

		public override string ToString()
		{
			return MyPosition.ToString("x");
		}

		public int CompareTo(ILLocation other)
		{
			return MyPosition.CompareTo(other.MyPosition);
		}
	}
}
namespace Ciloci.Flee
{

	/// <summary>
	/// Represents a branch from a start location to an end location
	/// </summary>
	internal class BranchInfo : IEquatable<BranchInfo>
	{

		private ILLocation MyStart;
		private ILLocation MyEnd;
		private Label MyLabel;

		private bool MyIsLongBranch;
		public BranchInfo(ILLocation startLocation, Label endLabel)
		{
			MyStart = startLocation;
			MyLabel = endLabel;
			MyEnd = new ILLocation();
		}

		public void AdjustForLongBranches(int longBranchCount)
		{
			MyStart.AdjustForLongBranch(longBranchCount);
		}

		public void BakeIsLongBranch()
		{
			MyIsLongBranch = this.ComputeIsLongBranch();
		}

		public void AdjustForLongBranchesBetween(int betweenLongBranchCount)
		{
			MyEnd.AdjustForLongBranch(betweenLongBranchCount);
		}

		public bool IsBetween(BranchInfo other)
		{
			return MyStart.CompareTo(other.MyStart) > 0 && MyStart.CompareTo(other.MyEnd) < 0;
		}

		public bool ComputeIsLongBranch()
		{
			return MyStart.IsLongBranch(MyEnd);
		}

		public void Mark(Label target, int position)
		{
			if (MyLabel.Equals(target) == true) {
				MyEnd.SetPosition(position);
			}
		}

		public bool Equals1(BranchInfo other)
		{
			return MyStart.Equals1(other.MyStart) && MyLabel.Equals(other.MyLabel);
		}
		bool System.IEquatable<BranchInfo>.Equals(BranchInfo other)
		{
			return Equals1(other);
		}

		public override string ToString()
		{
			return string.Format("{0} -> {1} (L={2})", MyStart, MyEnd, MyStart.IsLongBranch(MyEnd));
		}

		public bool IsLongBranch {
			get { return MyIsLongBranch; }
		}
	}
}
