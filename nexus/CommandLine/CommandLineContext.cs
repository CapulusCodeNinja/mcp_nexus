using System.CommandLine;
using System.Runtime.Versioning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using nexus.setup;
using nexus.setup.Configuration;
using nexus.setup.Models;
using nexus.setup.Interfaces;
using nexus.utilities.ServiceManagement;
using nexus.config.ServiceRegistration;
using nexus.Startup;
using nexus.Hosting;

namespace nexus.CommandLine;

/// <summary>
/// Builds the command-line interface for the application.
/// </summary>
internal class CommandLineBuilder
{
    private readonly string[] m_Args;

    public CommandLineBuilder(string[] args)
    {
        m_Args = args;
    }
}

