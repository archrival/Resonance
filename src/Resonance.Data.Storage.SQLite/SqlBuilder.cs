namespace Resonance.Data.Storage.SQLite
{
    public class SqlBuilder : Dapper.SqlBuilder
    {
        public new void AddClause(string name, string sql, object parameters = null, string joiner = "", string prefix = "", string postfix = "", bool isInclusive = false)
        {
            base.AddClause(name, sql, parameters, joiner, prefix, postfix, isInclusive);
        }

        public void AddSelect(string sql, dynamic parameters = null)
        {
            AddClause("addselect", sql, parameters);
        }

        public void Limit(string sql, dynamic parameters = null)
        {
            AddClause("limit", $"LIMIT {sql}", parameters);
        }

        public void Offset(string sql, dynamic parameters = null)
        {
            AddClause("offset", $"OFFSET {sql}", parameters);
        }

        public void PrimaryJoin(string sql, dynamic parameters = null)
        {
            AddClause("firstjoin", sql, parameters);
        }
    }
}