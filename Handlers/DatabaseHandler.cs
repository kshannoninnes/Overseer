using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Overseer.Handlers
{
    public class DatabaseHandler
    {
        private readonly string _dbPath;
        private readonly SQLiteAsyncConnection _conn;

        public DatabaseHandler(string filename) 
        {
            _dbPath = Path.Combine(Environment.CurrentDirectory, filename);
            _conn = new SQLiteAsyncConnection(_dbPath);
        }

        public async Task CreateTable<T>() where T: new()
        {
            await _conn.CreateTableAsync<T>();
        }

        public async Task InsertAsync<T>(T obj)
        {
            await _conn.InsertAsync(obj);
        } 

        public async Task UpdateAsync<T>(T obj)
        {
            await _conn.UpdateAsync(obj);
        }

        public async Task<T> GetAsync<T>(Expression<Func<T, bool>> predicate) where T: new()
        {
            var obj = await _conn.Table<T>().FirstOrDefaultAsync(predicate);
            return obj;
        }

        public async Task RemoveAsync<T>(T obj) where T: new()
        {
            var conn = new SQLiteAsyncConnection(_dbPath);
            await _conn.DeleteAsync(obj);
        }

        public async Task<bool> Exists<T>(Expression<Func<T, bool>> predicate) where T: new()
        {
            var doesExist = await _conn.Table<T>().CountAsync(predicate) > 0;
            return doesExist;
        }

        public async Task<List<T>> FindAll<T>(Expression<Func<T, bool>> predicate) where T: new()
        {
            var allObjs = await _conn.Table<T>().Where(predicate).ToListAsync();
            return allObjs;
        }
    }
}
