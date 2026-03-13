using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Polly;
using System;
using System.Threading.Tasks;

namespace API.Data
{
    /// <summary>
    /// 🌟✨ The Mighty Database Resilience Wizards ✨🌟
    /// These extension methods are like superhero capes for your database operations!
    /// They help your queries bounce back from failure like a cat landing on its feet.
    /// </summary>
    public static class ResilienceExtensions
    {
        /// <summary>
        /// 🛡️ The Shield of Database Resilience 🛡️
        /// Wraps your operation in a magical shield that protects against the dark forces of timeout exceptions
        /// and network gremlins. Even Gandalf would be impressed by this level of protection!
        /// </summary>
        /// <typeparam name="T">The treasure type you seek from your database quest</typeparam>
        /// <param name="dbContext">Your trusty database companion</param>
        /// <param name="operation">The perilous operation you wish to perform</param>
        /// <param name="policy">The ancient scrolls of retry wisdom</param>
        /// <param name="logger">The logger to record the adventure</param>
        /// <param name="operationKey">A magic spell to identify this particular operation</param>
        /// <returns>The result of your database adventure</returns>
        public static async Task<T> ExecuteResiliently<T>(
            this DbContext dbContext, 
            Func<Task<T>> operation, 
            IAsyncPolicy policy,
            ILogger logger,
            string? operationKey = null)
        {
            // Create a context dictionary for the policy
            var context = new Context(operationKey ?? Guid.NewGuid().ToString());
            
            try 
            {
                // Execute the operation with the policy
                return await policy.ExecuteAsync(async (ctx) => 
                {
                    try 
                    {
                        return await operation();
                    }
                    catch (Exception ex) when (IsTransientDatabaseException(ex))
                    {
                        logger.LogWarning(ex, "[DB Operation] 🚨 ALERT! Transient exception detected in {OperationKey}! 🚨 {ExceptionType}: {Message}", ctx.OperationKey, ex.GetType().Name, ex.Message);
                        throw; // Rethrow for Polly to handle with its retry policy
                    }
                }, context);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[DB Operation] 💥 The final boss defeated us after retries for {OperationKey}: {ExceptionType}: {Message}", operationKey, ex.GetType().Name, ex.Message);
                
                // Log inner exception details which often contain the root cause
                if (ex.InnerException != null)
                {
                    logger.LogError(ex.InnerException, "[DB Operation] 🕵️ Detective work: Inner exception found: {ExceptionType}: {Message}", ex.InnerException.GetType().Name, ex.InnerException.Message);
                }
                
                throw;
            }
        }
        
        /// <summary>
        /// 🧙‍♂️ The Void Wizard 🧙‍♂️
        /// Like its sibling above, but for operations that return nothing.
        /// They say the greatest wizards make things happen without leaving a trace.
        /// </summary>
        public static async Task ExecuteResiliently(
            this DbContext dbContext, 
            Func<Task> operation, 
            IAsyncPolicy policy,
            ILogger logger,
            string? operationKey = null)
        {
            // Create a context dictionary for the policy
            var context = new Context(operationKey ?? Guid.NewGuid().ToString());
            
            try
            {
                // Execute the operation with the policy
                await policy.ExecuteAsync(async (ctx) => 
                {
                    try 
                    {
                        await operation();
                    }
                    catch (Exception ex) when (IsTransientDatabaseException(ex))
                    {
                        logger.LogWarning(ex, "[DB Operation] 🚨 Caught transient exception in operation {OperationKey}! The database is playing hard to get today! {ExceptionType}: {Message}", ctx.OperationKey, ex.GetType().Name, ex.Message);
                        throw; // Rethrow for Polly to handle with its retry policy
                    }
                }, context);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[DB Operation] 💥 Database FATALITY! After valiant retries for {OperationKey}: {ExceptionType}: {Message}", operationKey, ex.GetType().Name, ex.Message);
                
                if (ex.InnerException != null)
                {
                    logger.LogError(ex.InnerException, "[DB Operation] 🔍 CSI Database: Inner exception revealed: {ExceptionType}: {Message}", ex.InnerException.GetType().Name, ex.InnerException.Message);
                }
                
                throw;
            }
        }
        
        /// <summary>
        /// 💾 The Grand Database Scribe 💾
        /// Ensures your changes are committed to the sacred scrolls of data persistence,
        /// even if the database server is having a bad hair day.
        /// </summary>
        public static async Task<int> SaveChangesResiliently(
            this DbContext dbContext, 
            IAsyncPolicy policy,
            ILogger logger)
        {
            try
            {
                return await dbContext.ExecuteResiliently(
                    () => dbContext.SaveChangesAsync(),
                    policy,
                    logger,
                    "SaveChanges_TheEpicSaga");
            }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "[DB Save] 📝 The database rejected our changes like a bad poetry submission! Error: {Message}", ex.Message);
                
                if (ex.InnerException != null) 
                {
                    logger.LogError(ex.InnerException, "[DB Save] 🔎 The plot thickens! Inner exception: {Message}", ex.InnerException.Message);
                }
                
                throw;
            }
        }
        
        /// <summary>
        /// 🔮 The Oracle of Database Exceptions 🔮
        /// Gazes into the crystal ball to determine if an exception is worthy of retrying.
        /// Some exceptions are just temporary glitches in the database matrix!
        /// </summary>
        private static bool IsTransientDatabaseException(Exception ex)
        {
            // Is it a timeout? Databases sometimes need a coffee break too
            if (ex is TimeoutException || 
                (ex.Message?.Contains("timeout", StringComparison.OrdinalIgnoreCase) ?? false))
            {
                return true; // Even databases need a breather sometimes
            }
            
            // Specialized database exceptions - the drama queens of exceptions
            if (ex is Microsoft.EntityFrameworkCore.DbUpdateException || 
                ex is Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
            {
                return true; // Classic database temper tantrums
            }
            
            // The Npgsql family of exceptions - quirky but lovable
            if (ex.GetType().FullName?.Contains("Npgsql") ?? false)
            {
                return true; // Postgres is just being Postgres
            }
            
            // Deep exception archeology - digging through the layers
            var currentEx = ex;
            while (currentEx != null)
            {
                if (currentEx is System.Net.Sockets.SocketException || 
                    currentEx is System.IO.IOException ||
                    (currentEx.Message?.Contains("connection", StringComparison.OrdinalIgnoreCase) ?? false))
                {
                    return true; // Network issues - blame it on solar flares
                }
                
                currentEx = currentEx.InnerException;
            }
            
            return false; // This exception means business - no retry for you!
        }
    }
}