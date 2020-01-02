using System;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace Overseer.Services.Misc
{
    public interface IDatabaseManager
    {
        public Task CreateTable<T>() where T : new();

        public Task InsertAsync<T>(T value);

        public Task UpdateAsync<T>(T value);

        public Task<T> GetAsync<T>(Expression<Func<T, bool>> predicate) where T : new();

        public Task RemoveAsync<T>(T value) where T : new();

        public Task<bool> Exists<T>(Expression<Func<T, bool>> predicate) where T : new();

        public Task<List<T>> FindAll<T>(Expression<Func<T, bool>> predicate) where T : new();
    }
}
