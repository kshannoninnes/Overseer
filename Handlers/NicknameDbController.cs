using Overseer.Models;
using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace Overseer.Handlers
{
    public class NicknameDbController
    {
        private readonly string _connString;

        public NicknameDbController(string connString)
        {
            _connString = connString;
        }

        public void Initialize()
        {
            CreateUserTable();
        }

        private void CreateUserTable()
        {
            using SQLiteConnection conn = new SQLiteConnection(_connString);
            conn.Open();

            string create = Schema.UserTable.Queries.CREATE_TABLE;
            SQLiteCommand cmd = conn.CreateCommand();
            cmd.CommandText = create;
            cmd.ExecuteNonQuery();
        }

        public void InsertUser(EnforcedUser user)
        {
            if(user.EnforcedNickname == null)
            {
                throw new ArgumentException($"A user's {nameof(user.EnforcedNickname)} cannot be null");
            }

            using SQLiteConnection conn = new SQLiteConnection(_connString);
            conn.Open();

            string insert = Schema.UserTable.Queries.INSERT;
            SQLiteCommand cmd = conn.CreateCommand();
            cmd.CommandText = insert;
            cmd.Parameters.Add(new SQLiteParameter("@id", user.Id.ToString()));
            cmd.Parameters.Add(new SQLiteParameter("@name", user.Nickname ?? string.Empty));
            cmd.Parameters.Add(new SQLiteParameter("@enforced", user.EnforcedNickname));
            cmd.ExecuteNonQuery();
        }

        public DiscordUser SelectUser(ulong id)
        {
            using var conn = new SQLiteConnection(_connString);
            conn.Open();

            string select = Schema.UserTable.Queries.SELECT_ONE;
            var cmd = conn.CreateCommand();
            cmd.CommandText = select;
            cmd.Parameters.Add(new SQLiteParameter("@id", id));

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
            {
                return null;
            }

            return new DiscordUser { Id = id, Nickname = reader.GetString(1) };
        }

        public Dictionary<ulong, EnforcedUser> SelectAllUsers()
        {
            using SQLiteConnection conn = new SQLiteConnection(_connString);
            conn.Open();

            var results = new Dictionary<ulong, EnforcedUser>();

            string read = Schema.UserTable.Queries.SELECT_ALL;
            SQLiteCommand cmd = conn.CreateCommand();
            cmd.CommandText = read;

            using SQLiteDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                // TODO Remove this null check after Weebfleet event over
                // Nulls were accidentally added to the db due to a bug
                // in the rename command and a lack of null checking in
                // the db insert method. Both were fixed.
                var user = new EnforcedUser
                {
                    Id = ulong.Parse(reader.GetString(0)),
                    Nickname = reader.GetString(1),
                    EnforcedNickname = reader.GetString(2)
                };

                results.Add(user.Id, user);
            }

            return results;
        }

        public void RemoveUser(ulong userId)
        {
            using var conn = new SQLiteConnection(_connString);
            conn.Open();

            string delete = Schema.UserTable.Queries.REMOVE;
            SQLiteCommand cmd = conn.CreateCommand();
            cmd.CommandText = delete;
            cmd.Parameters.Add(new SQLiteParameter("@id", userId));
            cmd.ExecuteNonQuery();
        }
    }
}