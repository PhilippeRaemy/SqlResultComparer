using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace ResultsComparer
{
    public class Results : Dictionary<string, Result>, IEquatable<Results>
    {
        public Results AddReader(SqlDataReader reader)
        {
            do
            {
                var resultName = reader.GetName(0);
                if (!ContainsKey(resultName)) Add(resultName, new Result());
                this[resultName].ColumnNames =
                    Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToArray();
                while (reader.Read())
                {
                    var rec = Enumerable.Range(0, reader.FieldCount).Select(e => (object)reader[e]).ToArray();
                    this[resultName].Add(rec);
                }
            } while (reader.NextResult());
            return this;
        }


        public bool Equals(Results other)
        {
            if (Count != other.Count) return false;
            if (Keys.Any(k => !other.ContainsKey(k))) return false;
            return Keys.All(k => this[k].Equals(other[k]));
        }

        public Results Diff(Results other, string[] ignoredColumns = null, int alignOnFirst = 0)
        {
            var diff = new Results();
            var equalMsg = new Result();
            equalMsg.ColumnNames=new []{"="};
            foreach (var key in Keys)
            {
                if (other.ContainsKey(key) && this[key].Equals(other[key]))
                {
                    diff.Add(key, equalMsg);
                }
                else
                {
                    diff.Add(key, !other.ContainsKey(key) ? this[key].Diff(new Result(), ignoredColumns) : this[key].Diff(other[key], ignoredColumns, alignOnFirst));
                }
            }
            foreach (var key in other.Keys.Where(k => !ContainsKey(k)))
            {
                diff.Add(key, new Result().Diff(other[key], ignoredColumns, alignOnFirst));
            }
            return diff;
        }
        public override string ToString()
        {
            return string.Join("\r\n\r\n",
                               this.Select(
                                   r =>
                                   string.Format("{0}\r\n{1}\r\n{0}\r\n{2}", new string('-', r.Key.Length), r.Key,
                                                 r.Value.ToString())));
        }
    }
}