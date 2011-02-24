using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Quartz;

using Common.Logging;


namespace JobAssembly
{
    public class SuperJob: IStatefulJob
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(SuperJob));

        #region IJob Members

        public void Execute(JobExecutionContext context)
        {

            logger.Info("SuperJob is launched");
        }

        #endregion
    }
}
