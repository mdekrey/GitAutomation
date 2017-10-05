using System;
using System.Collections.Generic;
using System.Text;

namespace GitAutomation.Auth
{
    public enum Permission
    {
        Read,
        Create,
        Delete,
        Update,
        Approve,
        Administrator,
    }

    public static class PolicyNames
    {
        public const string Read = "Read";
        public const string Delete = "Delete";
        public const string Create = "Create";
        public const string Update = "Update";
        public const string Approve = "Approve";
        public const string Administrate = "Administrate";
    }
}
