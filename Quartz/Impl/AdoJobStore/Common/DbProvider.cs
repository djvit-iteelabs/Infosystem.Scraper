using System;
using System.Collections;
using System.Collections.Specialized;
using System.Data;
using System.Globalization;
using System.Text;

using Quartz.Util;

namespace Quartz.Impl.AdoJobStore.Common
{
    /// <summary>
    ///     
    /// </summary>
    public class DbProvider : IDbProvider
    {
        protected const string PropertyDbProvider = "quartz.dbprovider";
        protected const string DbProviderResourceName = "Quartz.Impl.AdoJobStore.Common.dbproviders.properties";

        private string connectionString;
        private DbMetadata dbMetadata;

        protected static readonly Hashtable dbMetadataLookup = new Hashtable();
        protected static readonly object notInitializedMetadata = new object();

        static DbProvider()
        {
            // parse metadata
            PropertiesParser pp = PropertiesParser.ReadFromEmbeddedAssemblyResource(DbProviderResourceName);
            string[] providers = pp.GetPropertyGroups(PropertyDbProvider);
            foreach (string providerName in providers)
            {
                dbMetadataLookup[providerName] = notInitializedMetadata;
            }
        }

        ///<summary>
        /// Registers DB metadata information for given provider name.
        ///</summary>
        ///<param name="dbProviderName"></param>
        ///<param name="metadata"></param>
        public static void RegisterDbMetadata(string dbProviderName, DbMetadata metadata)
        {
            dbMetadataLookup[dbProviderName] = metadata;
         }

        protected virtual DbMetadata GetDbMetadata(string providerName)
        {
            if (dbMetadataLookup[providerName] == notInitializedMetadata)
            {
                try
                {
                    PropertiesParser pp =
                        PropertiesParser.ReadFromEmbeddedAssemblyResource(DbProviderResourceName);
                    DbMetadata metadata = new DbMetadata();
                    NameValueCollection props =
                        pp.GetPropertyGroup(PropertyDbProvider + "." + providerName, true);
                    ObjectUtils.SetObjectProperties(metadata, props);
                    metadata.Init();
                    RegisterDbMetadata(providerName, metadata);
                    return metadata;
                }
                catch (Exception ex)
                {
                    throw new Exception("Error while reading metadata information for provider '" + providerName + "'",
                                        ex);
                }
            }
            else
            {
                return (DbMetadata) dbMetadataLookup[providerName];
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DbProvider"/> class.
        /// </summary>
        /// <param name="dbProviderName">Name of the db provider.</param>
        /// <param name="connectionString">The connection string.</param>
        public DbProvider(string dbProviderName, string connectionString)
        {
            this.connectionString = connectionString;
            dbMetadata = GetDbMetadata(dbProviderName);

            if (dbMetadata == null)
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Invalid DB provider name: {0}{1}{2}", dbProviderName, Environment.NewLine, GenerateValidProviderNamesInfo()));
            }
        }

        protected static string GenerateValidProviderNamesInfo()
        {
            StringBuilder sb = new StringBuilder("Valid DB Provider names are:").Append(Environment.NewLine);
            foreach (string providerName in dbMetadataLookup.Keys)
            {
                sb.Append("\t").Append(providerName).Append(Environment.NewLine);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Returns a new command object for executing SQL statments/Stored Procedures
        /// against the database.
        /// </summary>
        /// <returns>An new <see cref="IDbCommand"/></returns>
        public virtual IDbCommand CreateCommand()
        {
            return (IDbCommand) ObjectUtils.InstantiateType(dbMetadata.CommandType); 
        }

        /// <summary>
        /// Returns a new instance of the providers CommandBuilder class.
        /// </summary>
        /// <returns>A new Command Builder</returns>
        /// <remarks>In .NET 1.1 there was no common base class or interface
        /// for command builders, hence the return signature is object to
        /// be portable (but more loosely typed) across .NET 1.1/2.0</remarks>
        public virtual object CreateCommandBuilder()
        {
            return ObjectUtils.InstantiateType(dbMetadata.CommandBuilderType); 
        }

        /// <summary>
        /// Returns a new connection object to communicate with the database.
        /// </summary>
        /// <returns>A new <see cref="IDbConnection"/></returns>
        public virtual IDbConnection CreateConnection()
        {
            IDbConnection conn = (IDbConnection)ObjectUtils.InstantiateType(dbMetadata.ConnectionType);
            conn.ConnectionString = ConnectionString;
            return conn;
        }

        /// <summary>
        /// Returns a new parameter object for binding values to parameter
        /// placeholders in SQL statements or Stored Procedure variables.
        /// </summary>
        /// <returns>A new <see cref="IDbDataParameter"/></returns>
        public virtual IDbDataParameter CreateParameter()
        {
            return (IDbDataParameter) ObjectUtils.InstantiateType(dbMetadata.ParameterType);
        }

        /// <summary>
        /// Connection string used to create connections.
        /// </summary>
        /// <value></value>
        public virtual string ConnectionString
        {
            get { return connectionString; }
            set { connectionString = value; }
        }

        /// <summary>
        /// Gets the metadata.
        /// </summary>
        /// <value>The metadata.</value>
        public virtual DbMetadata Metadata
        {
            get { return dbMetadata; }
        }

        /// <summary>
        /// Shutdowns this instance.
        /// </summary>
        public virtual void Shutdown()
        {

        }
    }
}
