using System.Collections;
using System.Data.SqlTypes;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Microsoft.SqlServer.Server;

internal class GroupNode
{
   private int _index;
   public int Index
   {
      get
      {
         return _index;
      }
   }
   private string _name;
   public string Name
   {
      get
      {
         return _name;
      }
   }
   private string _value;
   public string Value
   {
      get
      {
         return _value;
      }
   }

   public GroupNode( int index, string group, string value )
   {
      _index = index;
      _name = group;
      _value = value;
   }
}

internal class GroupIterator : IEnumerable
{
   private Regex _regex;
   private string _input;

   public GroupIterator( string input, string pattern )
   {
      _regex = new Regex( pattern, UserDefinedFunctions.Options );
      _input = input;
   }

   public IEnumerator GetEnumerator( )
   {
      int index = 0;
      Match current = null;
      string[] names = _regex.GetGroupNames( );
      while(current == null || current.Success)
      {
         index++;
         current = (current == null) ?
             _regex.Match( _input ) : current.NextMatch( );
         if(current.Success)
         {
            foreach(string name in names)
            {
               Group group = current.Groups[name];
               if(group.Success)
               {
                  yield return new GroupNode( index, name, group.Value );
               }
            }
         }
      }
   }
}

public static partial class UserDefinedFunctions
{
   [SqlFunction( FillRowMethodName = "FillGroupRow",
       TableDefinition = "[Index] int,[Group] nvarchar(max),[Text] nvarchar(max)" )]
   public static IEnumerable
       RegexGroups( SqlChars input, SqlString pattern )
   {
      return new GroupIterator( new string( input.Value ), pattern.Value );
   }

   [SuppressMessage( "Microsoft.Design", "CA1021:AvoidOutParameters" )]
   public static void FillGroupRow( object data,
       out SqlInt32 index, out SqlChars group, out SqlChars text )
   {
      GroupNode node = (GroupNode)data;
      index = new SqlInt32( node.Index );
      group = new SqlChars( node.Name.ToCharArray( ) );
      text = new SqlChars( node.Value.ToCharArray( ) );
   }
}