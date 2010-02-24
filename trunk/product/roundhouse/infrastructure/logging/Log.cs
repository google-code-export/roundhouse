namespace roundhouse.infrastructure.logging
{
    using System;
    using containers;
    using custom;
    using log4net;

    public static class Log
    {
        private static bool have_displayed_error_message;

        public static SubLogger bound_to(object object_that_needs_logging)
        {
            SubLogger logger;
            try
            {
                logger = Container.get_an_instance_of<LogFactory>().create_logger_bound_to(object_that_needs_logging);
            }
            catch(Exception)
            {
                if(!have_displayed_error_message)
                {
                    //Console.WriteLine(
                    //    "Creating the default logger with log4Net. Please register an LoggerFactory in the container if you would like to use something else.");
                    have_displayed_error_message = true;
                }
                logger = new Log4NetLogger(LogManager.GetLogger(object_that_needs_logging.ToString()));
            }

            return logger;
        }
    }
}