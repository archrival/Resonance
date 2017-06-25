namespace Resonance.Data.Storage.SQLite
{
    public class SqlBuilder : Dapper.SqlBuilder
    {
        public new void AddClause(string name, string sql, object parameters = null, string joiner = "", string prefix = "", string postfix = "", bool isInclusive = false)
        {
           base.AddClause(name, sql, parameters, joiner, prefix, postfix, isInclusive);
        }
    }
}
