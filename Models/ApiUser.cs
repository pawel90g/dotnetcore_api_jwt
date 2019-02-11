using System;
using Microsoft.AspNetCore.Identity;

public class ApiUser : IdentityUser
{
    public DateTime CreateDate { get; set; }
    public bool Verified { get; set; }
    public bool Del { get; set; }
    public DateTime? DeleteDateTime { get; set; }
    public string Company { get; set; }
    public string Address { get; set; }
    public string City { get; set; }
    public string Country { get; set; }
}