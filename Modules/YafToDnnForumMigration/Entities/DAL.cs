/*
MIT License

Copyright (c) Upendo Ventures, LLC

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
using DotNetNuke.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace Upendo.Modules.YafToDnnForumMigration.Entities
{
    public class DAL<T> where T : class
    {
        public static IEnumerable<T> Gets()
        {
            IEnumerable<T> t;
            using (IDataContext ctx = DataContext.Instance())
            {
                var rep = ctx.GetRepository<T>();
                t = rep.Get();
            }
            return t;
        }

        public static Task<IEnumerable<T>> GetsAsync()
        {
            return Task.Run(() =>
            {
                IEnumerable<T> t;
                using (IDataContext ctx = DataContext.Instance())
                {
                    var rep = ctx.GetRepository<T>();
                    t = rep.Get();
                }
                return t;
            });
        }

        /// <summary>
        /// Get single object from the database
        /// </summary>
        /// <returns>
        /// The object
        /// </returns>
        /// <param name='Id'>
        /// Item identifier.
        /// </param>
        public static T Get(int Id)
        {
            T t;
            using (IDataContext ctx = DataContext.Instance())
            {
                var rep = ctx.GetRepository<T>();
                t = rep.GetById(Id);
            }
            return t;
        }

        public static T Get(long Id)
        {
            T t;
            using (IDataContext ctx = DataContext.Instance())
            {
                var rep = ctx.GetRepository<T>();
                t = rep.GetById(Id);
            }
            return t;
        }

        public static Task<T> GetAsync(int Id)
        {
            return Task.Run(() =>
            {
                using (IDataContext ctx = DataContext.Instance())
                {
                    var rep = ctx.GetRepository<T>();
                    return rep.GetById(Id);
                }
            });
        }

        /// <summary>
        /// Get single object from the database
        /// </summary>
        /// <returns>
        /// The object
        /// </returns>
        /// <param name='id'>
        /// Item identifier.
        /// </param>
        /// <param name='scopeId'>
        /// Scope identifier (like moduleId)
        /// </param>
        public static T Get(int id, int scopeId)
        {
            T info;

            using (var ctx = DataContext.Instance())
            {
                var repo = ctx.GetRepository<T>();
                info = repo.GetById(id, scopeId);
            }

            return info;
        }

        public static Task<T> GetAsync(int id, int scopeId)
        {
            return Task.Run(() =>
            {
                using (var ctx = DataContext.Instance())
                {
                    var repo = ctx.GetRepository<T>();
                    return repo.GetById(id, scopeId);
                }
            });
        }

        /// <summary>
        /// Gets a single object of type T from result of a dynamic sql query
        /// </summary>
        /// <returns>Object of type T or null.</returns>
        /// <param name="sqlCondition">SQL command condition.</param>
        /// <param name="args">SQL command arguments.</param>
        /// <typeparam name="T">Type of objects.</typeparam>
        public static T Get(string sqlCondition, params object[] args)
        {
            T info;

            using (var ctx = DataContext.Instance())
            {
                var repo = ctx.GetRepository<T>();
                info = repo.Find(sqlCondition, args).SingleOrDefault();
            }

            return info;
        }

        public static Task<T> GetAsync(string sqlCondition, params object[] args)
        {
            return Task.Run(() =>
            {
                using (var ctx = DataContext.Instance())
                {
                    var repo = ctx.GetRepository<T>();
                    return repo.Find(sqlCondition, args).SingleOrDefault();
                }
            });
        }

        /// <summary>
        /// Gets the all objects of type T from result of a dynamic sql query
        /// </summary>
        /// <returns>Enumerable with objects of type T</returns>
        /// <param name="cmdType">Type of an SQL command.</param>
        /// <param name="sql">SQL command.</param>
        /// <param name="args">SQL command arguments.</param>
        /// <typeparam name="T">Type of objects.</typeparam>
        public static IEnumerable<T> Gets(System.Data.CommandType cmdType, string sql, params object[] args)
        {
            IEnumerable<T> infos;

            using (var ctx = DataContext.Instance())
            {
                infos = ctx.ExecuteQuery<T>(cmdType, sql, args);
            }

            return infos ?? Enumerable.Empty<T>();
        }

        public static Task<IEnumerable<T>> GetsAsync(System.Data.CommandType cmdType, string sql, params object[] args)
        {
            return Task.Run(() =>
            {
                IEnumerable<T> infos;

                using (var ctx = DataContext.Instance())
                {
                    infos = ctx.ExecuteQuery<T>(cmdType, sql, args);
                }

                return infos ?? Enumerable.Empty<T>();
            });
        }

        /// <summary>
        /// Gets the all objects of type T from result of a stored procedure call
        /// </summary>
        /// <returns>Enumerable with objects of type T</returns>
        /// <param name="spName">Stored procedure name.</param>
        /// <param name="args">SQL command arguments.</param>
        /// <typeparam name="T">Type of objects.</typeparam>
        public static IEnumerable<T> GetsFromSp(string spName, params object[] args)
        {
            IEnumerable<T> infos;

            using (var ctx = DataContext.Instance())
            {
                infos = ctx.ExecuteQuery<T>(System.Data.CommandType.StoredProcedure, spName, args);
            }

            return infos ?? Enumerable.Empty<T>();
        }

        /// <summary>
        /// Gets stored procedure execution result as scalar value
        /// </summary>
        /// <returns>Stored procedure execution result as scalar value.</returns>
        /// <param name="spName">Stored procedure name.</param>
        /// <param name="args">SQL command arguments.</param>
        /// <typeparam name="T">Type of scalar.</typeparam>
        public static T ExecuteSpScalar(string spName, params object[] args)
        {
            using (var ctx = DataContext.Instance())
            {
                return ctx.ExecuteScalar<T>(System.Data.CommandType.StoredProcedure, spName, args);
            }
        }

        public static IQueryable<T> Find(string sqlCondition, params object[] args)
        {
            IQueryable<T> t;
            using (IDataContext ctx = DataContext.Instance())
            {
                var rep = ctx.GetRepository<T>();
                t = rep.Find(sqlCondition, args).AsQueryable();
            }
            return t;
        }

        public static T Create(T t)
        {
            using (IDataContext ctx = DataContext.Instance())
            {
                var rep = ctx.GetRepository<T>();
                rep.Insert(t);
            }
            return t;
        }

        public static T Update(T t)
        {
            using (IDataContext ctx = DataContext.Instance())
            {
                var rep = ctx.GetRepository<T>();
                rep.Update(t);
            }
            return t;
        }

        public static void Delete(int Id)
        {
            var t = Get(Id);
            Delete(t);
        }

        public static void Delete(long Id)
        {
            var t = Get(Id);
            Delete(t);
        }

        public static void Delete(T t)
        {
            using (IDataContext ctx = DataContext.Instance())
            {
                var rep = ctx.GetRepository<T>();
                rep.Delete(t);
            }
        }

        public static IEnumerable<T> ExecuteQuery(System.Data.CommandType cmdType, string sql, params object[] args)
        {
            IEnumerable<T> infos;

            using (var ctx = DataContext.Instance())
            {
                infos = ctx.ExecuteQuery<T>(cmdType, sql, args);
            }

            return infos ?? Enumerable.Empty<T>();
        }

        public static Task<IEnumerable<T>> ExecuteQueryAsync(System.Data.CommandType cmdType, string sql, params object[] args)
        {
            return Task.Run(() =>
            {
                IEnumerable<T> infos;

                using (var ctx = DataContext.Instance())
                {
                    infos = ctx.ExecuteQuery<T>(cmdType, sql, args);
                }

                return infos ?? Enumerable.Empty<T>();
            });
        }

        public static T ExecuteSingleOrDefault(System.Data.CommandType cmdType, string sql, params object[] args)
        {
            T infos;

            using (var ctx = DataContext.Instance())
            {
                infos = ctx.ExecuteSingleOrDefault<T>(cmdType, sql, args);
            }

            return infos;
        }
    }
}