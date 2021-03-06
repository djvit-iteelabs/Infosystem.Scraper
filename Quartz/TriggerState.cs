/* 
* Copyright 2004-2009 James House 
* 
* Licensed under the Apache License, Version 2.0 (the "License"); you may not 
* use this file except in compliance with the License. You may obtain a copy 
* of the License at 
* 
*   http://www.apache.org/licenses/LICENSE-2.0 
*   
* Unless required by applicable law or agreed to in writing, software 
* distributed under the License is distributed on an "AS IS" BASIS, WITHOUT 
* WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the 
* License for the specific language governing permissions and limitations 
* under the License.
* 
*/

namespace Quartz
{
    /// <summary>
    /// All trigger states known to Scheduler.
    /// </summary>
   public enum TriggerState
    {
        /// <summary>
        /// Indicates that the <see cref="Trigger" /> is in the "normal" state.
        /// </summary>
        Normal,

	   /// <summary>
        /// Indicates that the <see cref="Trigger" /> is in the "paused" state.
        /// </summary>
        Paused,

        /// <summary>
        /// Indicates that the <see cref="Trigger" /> is in the "complete" state.
        /// </summary>
        /// <remarks>
        /// "Complete" indicates that the trigger has not remaining fire-times in
        /// its schedule.
        /// </remarks>
        Complete,

        /// <summary>
        /// Indicates that the <see cref="Trigger" /> is in the "error" state.
        /// </summary>
        /// <remarks>
        /// <p>
        /// A <see cref="Trigger" /> arrives at the error state when the scheduler
        /// attempts to fire it, but cannot due to an error creating and executing
        /// its related job. Often this is due to the <see cref="IJob" />'s
        /// class not existing in the classpath.
        /// </p>
        /// 
        /// <p>
        /// When the trigger is in the error state, the scheduler will make no
        /// attempts to fire it.
        /// </p>
        /// </remarks>
        Error,

        /// <summary>
        /// Indicates that the <see cref="Trigger" /> is in the "blocked" state.
        /// </summary>
        /// <remarks>
        /// A <see cref="Trigger" /> arrives at the blocked state when the job that
        /// it is associated with is a <see cref="IStatefulJob" /> and it is 
        /// currently executing.
        /// </remarks>
        /// <seealso cref="IStatefulJob" />
        Blocked,

		/// <summary>
        /// Indicates that the <see cref="Trigger" /> does not exist.
        /// </summary>
        None
    }
}
