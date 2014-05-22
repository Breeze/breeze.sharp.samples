namespace FooBar.Models
{
    public class Foo
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string SomethingVeryBig { get; set; }
        public RoleType TheRoleType { get; set; }

        public enum RoleType
        {
            Guest = 0,
            Restricted = 1,
            Standard = 2,
            Admin = 3
        }
    }
}
