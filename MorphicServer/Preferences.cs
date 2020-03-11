namespace MorphicServer
{

    /// <summary>Dummy data model for preferences</summary>
    public class Preferences
    {

        public Preferences(string id){
            this.id = id;
        }

        public Preferences(){
            this.id = "";
        }

        public string id { get; set; }
    }
}