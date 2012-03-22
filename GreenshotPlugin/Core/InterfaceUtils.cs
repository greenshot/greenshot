/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2012  Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on Sourceforge: http://sourceforge.net/projects/greenshot/
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */
using System;
using System.Threading;
using System.Reflection;
using System.Collections.Generic;

using Greenshot.Plugin;

namespace GreenshotPlugin.Core {
	/// <summary>
	/// Description of InterfaceUtils.
	/// </summary>
	public static class InterfaceUtils {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(InterfaceUtils));

		public static List<Type> GetSubclassesOf(Type type, bool excludeSystemTypes) {
			List<Type> list = new List<Type>();
			foreach(Assembly currentAssembly in Thread.GetDomain().GetAssemblies()) {
				try {
					Type[] types = currentAssembly.GetTypes();
					if (!excludeSystemTypes || (excludeSystemTypes && !currentAssembly.FullName.StartsWith("System."))) {
						foreach(Type currentType in types) {
							if (type.IsInterface) {
								if (currentType.GetInterface(type.FullName) != null) {
									list.Add(currentType);
								}
							} else if (currentType.IsSubclassOf(type)) {
								list.Add(currentType);
							}
						}
					}
				} catch (Exception ex) {
					LOG.Warn(ex);
				}
			}
			return list;
		}

		public static List<IProcessor> GetProcessors() {
			List<IProcessor> processors = new List<IProcessor>();
			foreach(Type processorType in GetSubclassesOf(typeof(IProcessor), true)) {
				if (!processorType.IsAbstract) {
					IProcessor processor = (IProcessor)Activator.CreateInstance(processorType);
					if (processor.isActive) {
						LOG.DebugFormat("Found processor {0} with designation {1}", processorType.Name, processor.Designation);
						processors.Add(processor);						
					} else {
						LOG.DebugFormat("Ignoring processor {0} with designation {1}", processorType.Name, processor.Designation);
					}
				}
			}
			return processors;
		}
	}
}
