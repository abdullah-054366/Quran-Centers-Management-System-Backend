using Data_Access_Layer.Data;

namespace Business_Layer
{
    public class DatabaseCheck
    {

        static public bool TestDatabase()
        {
            return DatabaseContext.TestDatabase();
        }



    }
}
