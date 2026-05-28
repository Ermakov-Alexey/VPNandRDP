namespace VaR;

public class Contact
{
    public int ID
    {
        get; set;
    }
    public string Name
    {
        get; set;
    }
    private string _email;
    public string Email
    {
        get => _email;
        set
        {
            _email = value;
            if (value != "")
            {
                Login = value.Contains("@") ? value.Split('@')[0] : value;
            }
        }
    }
    public string Login
    {
        get; set;
    }
    public string VPNUser
    {
        get; set;
    }
    public string VPNPassword
    {
        get; set;
    }
    public Contact()
    {
        ID = 0;
        Name = "";
        Email = "";
    }
    public Contact(int id, string name, string email)
    {
        ID = id;
        Name = name;
        Email = email;
    }
}