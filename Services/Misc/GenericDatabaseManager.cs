using SQLite;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace Overseer.Services.Misc
{
    public class GenericDatabaseManager : IDatabaseManager
    {
        private readonly SQLiteAsyncConnection _conn;

        public GenericDatabaseManager(string filename)
        {
            var dbPath = Path.Combine(Environment.CurrentDirectory, filename);
            _conn = new SQLiteAsyncConnection(dbPath);
        }

        public async Task CreateTable<T>() where T : new()
        {
            await _conn.CreateTableAsync<T>();
        }

        public async Task InsertAsync<T>(T value)
        {
            await _conn.InsertAsync(value);
        }

        public async Task UpdateAsync<T>(T value)
        {
            await _conn.UpdateAsync(value);
        }

        public async Task<T> GetAsync<T>(Expression<Func<T, bool>> predicate) where T : new()
        {
            var obj = await _conn.Table<T>().FirstOrDefaultAsync(predicate);
            return obj;
        }

        public async Task RemoveAsync<T>(T value) where T : new()
        {
            await _conn.DeleteAsync(value);
        }

        public async Task<bool> Exists<T>(Expression<Func<T, bool>> predicate) where T : new()
        {
            var doesExist = await _conn.Table<T>().CountAsync(predicate) > 0;
            return doesExist;
        }

        public async Task<List<T>> FindAll<T>(Expression<Func<T, bool>> predicate) where T : new()
        {
            var allObjs = await _conn.Table<T>().Where(predicate).ToListAsync();
            return allObjs;
        }
    }
}
