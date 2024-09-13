using DevExpress.XtraSpreadsheet.Model;
using InvEntry.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;

namespace InvEntry.Store;

public class FormulaStore
{
    private static FormulaStore _formulaStore;

    private FormulaStore() { _storage = new(); }

    public static FormulaStore Instance 
    { 
        get 
        {
            if(_formulaStore is null) _formulaStore = new FormulaStore();

            return _formulaStore;
        } 
    }

    Dictionary<string, List<Formula>> _storage;

    public void AddFormula<TSource>(Expression<Func<TSource, object>> lamdaExpression, string expression) 
        where TSource : class
    {
        AddFormula<TSource>(lamdaExpression.GetMemberName(), expression);
    }

    public void AddFormula<T>(string fieldName, string expression) where T : class
    {
        var formula = Formula.Create<T>(fieldName, expression);

        var key = typeof(T).Name;

        if (_storage.ContainsKey(key))
        {
            _storage[key].Add(formula);
        }
        else
        {
            _storage[key] = new List<Formula> { formula };
        }
    }

    public IEnumerable<Formula> GetFormulas<T>() where T : class 
    {
        var key = typeof(T).Name;

        return GetFormulas(key);
    }

    public IEnumerable<Formula> GetFormulas(string key)
    {
        if (_storage.ContainsKey(key))
        {
            return _storage[key];
        }

        return Enumerable.Empty<Formula>();
    }

    public Formula? GetFormula(string key, string fieldName)
    {
        if (_storage.ContainsKey(key))
        {
            return _storage[key].FirstOrDefault(x => x.FieldName.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
        }

        return null;
    }

    public Formula? GetFormula<T>(string fieldName) where T : class
    {
        var key = typeof(T).Name;

        return GetFormula(key, fieldName);
    }

    public bool TryGetFormula<T>(string fieldName, out Formula? formula) where T : class
    {
        formula = GetFormula<T>(fieldName);
        return formula != null;
    }

    public bool TryGetFormula(string key, string fieldName, out Formula? formula)
    {
        formula = GetFormula(key, fieldName);
        return formula != null;
    }
}

public class Formula
{
    public static Formula Create<T>(string fieldName, string expression) where T : class
        => new(fieldName, expression, typeof(T));

    public static Formula Create(string fieldName, string expression, Type type)
    => new(fieldName, expression, type);

    private Formula(string fieldName, string expression, Type type) 
    {
        FieldName = fieldName;
        Expression = expression;
        Type = type;
    }
    public string FieldName;
    public string Expression;
    public Type Type;
}
