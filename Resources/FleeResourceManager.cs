
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Resources;

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

	internal class FleeResourceManager
	{

		private Dictionary<string, ResourceManager> MyResourceManagers;

		private static FleeResourceManager OurInstance = new FleeResourceManager();
		private FleeResourceManager()
		{
			MyResourceManagers = new Dictionary<string, ResourceManager>(StringComparer.OrdinalIgnoreCase);
		}

		private ResourceManager GetResourceManager(string resourceFile)
		{
			lock (this) {
				ResourceManager rm = null;
				if (MyResourceManagers.TryGetValue(resourceFile, out rm) == false) {
					Type t = typeof(FleeResourceManager);
					rm = new ResourceManager(string.Format("{0}.{1}", t.Namespace, resourceFile), t.Assembly);
					MyResourceManagers.Add(resourceFile, rm);
				}
				return rm;
			}
		}

		private string GetResourceString(string resourceFile, string key)
		{
			ResourceManager rm = this.GetResourceManager(resourceFile);
			return rm.GetString(key);
		}

		public string GetCompileErrorString(string key)
		{
			return this.GetResourceString("CompileErrors", key);
		}

		public string GetElementNameString(string key)
		{
			return this.GetResourceString("ElementNames", key);
		}

		public string GetGeneralErrorString(string key)
		{
			return this.GetResourceString("GeneralErrors", key);
		}

		public static FleeResourceManager Instance {
			get { return OurInstance; }
		}
	}
}
