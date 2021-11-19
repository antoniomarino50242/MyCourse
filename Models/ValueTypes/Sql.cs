namespace MyCourse.Models.ValueTypes
{
    public class Sql
    {
        //Questa classe serve unicamente per indicare al servizio infrastrutturale SqliteAccessor
        //che un dato parametro non deve essere convertito in SqliteParameter
        private Sql(string value)
        {
            this.Value = value;
        }
        //prprietà per conservare il tipo originale
        public string Value { get; }

        //conversione da/per il tipo
        public static explicit operator Sql(string value) => new Sql(value);

        public override string ToString()
        {
            return this.Value;
        }
    }
}