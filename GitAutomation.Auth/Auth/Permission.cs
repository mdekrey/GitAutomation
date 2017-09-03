using System;
using System.Collections.Generic;
using System.Text;

namespace GitAutomation.Auth
{
    public enum Permission
    {
        Read,
        Delete,
        Update,
        Approve,
        Administrator,
    }

    public static class PolicyNames
    {
        public const string Read = "Read";
        public const string Delete = "Delete";
        public const string Update = "Update";
        public const string Approve = "Approve";
        public const string Administrate = "Administrate";
    }
}
