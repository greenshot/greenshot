/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2020 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
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
using System.Collections.Generic;

using Greenshot.Plugin;
using GreenshotPlugin.Core;
using log4net;

namespace Greenshot.Helpers {
	/// <summary>
	/// Description of ProcessorHelper.
	/// </summary>
	public static class ProcessorHelper {
		private static readonly ILog LOG = LogManager.GetLogger(typeof(ProcessorHelper));

		/// <summary>
		/// Register the internal processors
		/// </summary>
		public static void RegisterInternalProcessors() {
			foreach(Type processorType in InterfaceUtils.GetSubclassesOf(typeof(IProcessor),true)) {
				// Only take our own
				if (!"Greenshot.Processors".Equals(processorType.Namespace)) {
					continue;
				}
				try {
					if (!processorType.IsAbstract) {
						IProcessor processor;
						try {
							processor = (IProcessor)Activator.CreateInstance(processorType);
						} catch (Exception e) {
							LOG.ErrorFormat("Can't create instance of {0}", processorType);
							LOG.Error(e);
							continue;
						}
						if (processor.isActive) {
							LOG.DebugFormat("Found Processor {0} with designation {1}", processorType.Name, processor.Designation);
							SimpleServiceProvider.Current.AddService(processor);
						} else {
							LOG.DebugFormat("Ignoring Processor {0} with designation {1}", processorType.Name, processor.Designation);
						}
					}
				} catch (Exception ex) {
					LOG.ErrorFormat("Error loading processor {0}, message: ", processorType.FullName, ex.Message);
				}
			}
		}
	}
}
