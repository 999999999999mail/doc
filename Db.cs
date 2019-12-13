using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FF.Lotto.Model
{
    public class Db
    {
        private readonly Func<SqlConnection> _dbConnectionFactory;

        public Db(Func<SqlConnection> dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory ?? throw new ArgumentNullException(nameof(dbConnectionFactory));
        }

        public async Task<T> CommandAsync<T>(Func<SqlConnection, SqlTransaction, int, Task<T>> command)
        {
            using (var connection = _dbConnectionFactory.Invoke())
            {
                await connection.OpenAsync();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        var result = await command(connection, transaction, 20);
                        transaction.Commit();
                        return result;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw ex;
                    }
                }
            }
        }

        public async Task<T> GetAsync<T>(Func<SqlConnection, SqlTransaction, int, Task<T>> command)
        {
            return await CommandAsync(command);
        }

        public async Task<IList<T>> SelectAsync<T>(Func<SqlConnection, SqlTransaction, int, Task<IList<T>>> command)
        {
            return await CommandAsync(command);
        }

        public async Task ExecuteAsync(string sql, object parameters)
        {
            await CommandAsync(async (conn, trn, timeout) =>
            {
                var num = await conn.ExecuteAsync(sql, parameters, trn, timeout);
                return num;
            });
        }

        public async Task<T> GetAsync<T>(string sql, object parameters)
        {
            return await CommandAsync(async (conn, trn, timeout) =>
            {
                T result = await conn.QuerySingleAsync<T>(sql, parameters, trn, timeout);
                return result;
            });
        }

        public async Task<IList<T>> SelectAsync<T>(string sql, object parameters)
        {
            return await CommandAsync<IList<T>>(async (conn, trn, timeout) =>
            {
                var result = (await conn.QueryAsync<T>(sql, parameters, trn, timeout)).ToList();
                return result;
            });
        }
    }
}
