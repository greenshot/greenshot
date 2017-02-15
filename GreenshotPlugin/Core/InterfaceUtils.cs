#region Dapplo 2017 - GNU Lesser General Public License

// Dapplo - building blocks for .NET applications
// Copyright (C) 2017 Dapplo
// 
// For more information see: http://dapplo.net/
// Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
// This file is part of Greenshot
// 
// Greenshot is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Greenshot is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have a copy of the GNU Lesser General Public License
// along with Greenshot. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

#endregion

#region Usings

using System;
using System.Collections.Generic;
using System.Threading;
using GreenshotPlugin.Interfaces;
using log4net;

#endregion

namespace GreenshotPlugin.Core
{
	/// <summary>
	///     Description of InterfaceUtils.
	/// </summary>
	public static class InterfaceUtils
	{
		private static readonly ILog LOG = LogManager.GetLogger(typeof(InterfaceUtils));

		public static List<Type> GetSubclassesOf(Type type, bool excludeSystemTypes)
		{
			var list = new List<Type>();
			foreach (var currentAssembly in Thread.GetDomain().GetAssemblies())
			{
				try
				{
					var types = currentAssembly.GetTypes();
					if (!excludeSystemTypes || excludeSystemTypes && !currentAssembly.FullName.StartsWith("System."))
					{
						foreach (var currentType in types)
						{
							if (type.IsInterface)
							{
								if (currentType.GetInterface(type.FullName) != null)
								{
									list.Add(currentType);
								}
							}
							else if (currentType.IsSubclassOf(type))
							{
								list.Add(currentType);
							}
						}
					}
				}
				catch (Exception ex)
				{
					LOG.WarnFormat("Problem getting subclasses of type: {0}, message: {1}", type.FullName, ex.Message);
				}
			}
			return list;
		}

		public static List<IProcessor> GetProcessors()
		{
			var processors = new List<IProcessor>();
			foreach (var processorType in GetSubclassesOf(typeof(IProcessor), true))
			{
				if (!processorType.IsAbstract)
				{
					var processor = (IProcessor) Activator.CreateInstance(processorType);
					if (processor.isActive)
					{
						LOG.DebugFormat("Found processor {0} with designation {1}", processorType.Name, processor.Designation);
						processors.Add(processor);
					}
					else
					{
						LOG.DebugFormat("Ignoring processor {0} with designation {1}", processorType.Name, processor.Designation);
					}
				}
			}
			return processors;
		}
	}
}