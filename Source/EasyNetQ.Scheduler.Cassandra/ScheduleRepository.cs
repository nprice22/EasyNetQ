using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using EasyNetQ.SystemMessages;
using Newtonsoft.Json;
using Cassandra;

namespace EasyNetQ.Scheduler
{
    public interface IScheduleRepository
    {
        void Store(ScheduleMe scheduleMe);
        void Cancel(UnscheduleMe unscheduleMe);
        IList<ScheduleMe> GetPending();
        void Purge();
    }

    public class ScheduleRepository : IScheduleRepository
    {
        private readonly ScheduleRepositoryConfiguration configuration;
        private readonly Func<DateTime> now;
        private ISession session;

        public ScheduleRepository(ScheduleRepositoryConfiguration configuration, Func<DateTime> now)
        {
            this.configuration = configuration;
            this.now = now;
            this.session = GetClusterConnection(configuration.ConnectionString);
        }

        public void Store(ScheduleMe scheduleMe)
        {
            //WithStoredProcedureCommand(dialect.InsertProcedureName, command =>
            //{
            //    AddParameter(command, dialect.WakeTimeParameterName, scheduleMe.WakeTime, DbType.DateTime);
            //    AddParameter(command, dialect.BindingKeyParameterName, scheduleMe.BindingKey, DbType.String);
            //    AddParameter(command, dialect.CancellationKeyParameterName, scheduleMe.CancellationKey, DbType.String);
            //    AddParameter(command, dialect.MessageParameterName, scheduleMe.InnerMessage, DbType.Binary);
            //    AddParameter(command, dialect.ExchangeParameterName, scheduleMe.Exchange, DbType.String);
            //    AddParameter(command, dialect.ExchangeTypeParameterName, scheduleMe.ExchangeType, DbType.String);
            //    AddParameter(command, dialect.RoutingKeyParameterName, scheduleMe.RoutingKey, DbType.String);
            //    AddParameter(command, dialect.MessagePropertiesParameterName, SerializeToString(scheduleMe.MessageProperties), DbType.String);
            //    AddParameter(command, dialect.InstanceNameParameterName, configuration.InstanceName, DbType.String);

            //    command.ExecuteNonQuery();
            //});
        }

        public void Cancel(UnscheduleMe unscheduleMe)
        {
            //ThreadPool.QueueUserWorkItem(state =>
            //    WithStoredProcedureCommand(dialect.CancelProcedureName, command =>
            //    {
            //        AddParameter(command, dialect.CancellationKeyParameterName, unscheduleMe.CancellationKey, DbType.String);
    
            //        command.ExecuteNonQuery();
            //    })
            //);
        }

        public IList<ScheduleMe> GetPending()
        {
            //var scheduledMessages = new List<ScheduleMe>();
            //var scheduleMessageIds = new List<int>();

            //WithStoredProcedureCommand(dialect.SelectProcedureName, command =>
            //{
            //    var dateTime = now();
            //    AddParameter(command, dialect.RowsParameterName, configuration.MaximumScheduleMessagesToReturn, DbType.Int32);
            //    AddParameter(command, dialect.StatusParameterName, 0, DbType.Int16);
            //    AddParameter(command, dialect.WakeTimeParameterName, dateTime, DbType.DateTime);
            //    AddParameter(command, dialect.InstanceNameParameterName, configuration.InstanceName ?? "", DbType.String);

            //    using (var reader = command.ExecuteReader())
            //    {
            //        while (reader.Read())
            //        {
            //            scheduledMessages.Add(new ScheduleMe
            //            {
            //                WakeTime = (DateTime) reader["WakeTime"],
            //                BindingKey = reader["BindingKey"].ToString(),
            //                InnerMessage = (byte[])reader["InnerMessage"],
            //                CancellationKey = reader["CancellationKey"].ToString(),
            //                Exchange = reader["Exchange"].ToString(),
            //                ExchangeType = reader["ExchangeType"].ToString(),
            //                RoutingKey = reader["RoutingKey"].ToString(),
            //                MessageProperties = DeserializeToMessageProperties(reader["MessageProperties"].ToString()),
            //            });

            //            scheduleMessageIds.Add((int)reader["WorkItemId"]);
            //        }
            //    }
            //});

            //MarkItemsForPurge(scheduleMessageIds);

            //return scheduledMessages;
            throw new NotImplementedException();
        }

        public void MarkItemsForPurge(IEnumerable<int> scheduleMessageIds)
        {
            // mark items for purge on a background thread.
            //ThreadPool.QueueUserWorkItem(state =>
            //    WithStoredProcedureCommand(dialect.MarkForPurgeProcedureName, command =>
            //    {
            //        var purgeDate = now().AddDays(configuration.PurgeDelayDays);

            //        var idParameter = AddParameter(command, dialect.IdParameterName, DbType.Int32);
            //        AddParameter(command, dialect.PurgeDateParameterName, purgeDate, DbType.DateTime);

            //        foreach (var scheduleMessageId in scheduleMessageIds)
            //        {
            //            idParameter.Value = scheduleMessageId;
            //            command.ExecuteNonQuery();
            //        }
            //    })
            //);
        }

        private static MessageProperties DeserializeToMessageProperties(string properties)
        {
            // backwards compatibility with older messages
            if (string.IsNullOrWhiteSpace(properties))
                return null;
            return JsonConvert.DeserializeObject<MessageProperties>(properties);
        }

        private static string SerializeToString(MessageProperties properties)
        {
            if (properties == null)
                throw new ArgumentNullException("properties");
            return JsonConvert.SerializeObject(properties);
        }

        public void Purge()
        {
            //WithStoredProcedureCommand(dialect.PurgeProcedureName, command =>
            //    {
            //        AddParameter(command, dialect.RowsParameterName, configuration.PurgeBatchSize, DbType.Int16);
            //        AddParameter(command, dialect.PurgeDateParameterName, now(), DbType.DateTime);

            //        command.ExecuteNonQuery();
            //    });
        }


        private string FormatWithSchemaName(string storedProcedureName)
        {
            if (string.IsNullOrWhiteSpace(configuration.SchemaName))
                return storedProcedureName;

            return string.Format("[{0}].{1}", configuration.SchemaName.TrimStart('[').TrimEnd('.', ']'), storedProcedureName);
        }

        private ISession GetClusterConnection(string node)
        {
            var scheduleCluster = Cluster.Builder().AddContactPoint(node).Build();
            Console.WriteLine("Connected to cluster: " + scheduleCluster.Metadata.ClusterName.ToString());
            foreach (var host in scheduleCluster.Metadata.AllHosts())
            {
                Console.WriteLine("Data Center: " + host.Datacenter + ", " +
                    "Host: " + host.Address + ", " +
                    "Rack: " + host.Rack);
            }
            return scheduleCluster.Connect();
        }

        private IDbCommand CreateCommand(IDbConnection connection, string commandText)
        {
            var command = connection.CreateCommand();
            command.CommandText = commandText;
            return command;
        }

        private void AddParameter(IDbCommand command, string parameterName, object value, DbType dbType)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = parameterName;
            parameter.Value = value;
            parameter.DbType = dbType;
            command.Parameters.Add(parameter);
        }

        private IDbDataParameter AddParameter(IDbCommand command, string parameterName, DbType dbType)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = parameterName;
            parameter.DbType = dbType;
            command.Parameters.Add(parameter);
            return parameter;
        }
    }
}