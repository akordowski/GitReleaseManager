﻿//-----------------------------------------------------------------------
// <copyright file="CreateConfig.cs" company="gep13">
//     Copyright (c) gep13. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace GitHubReleaseManager.Configuration
{
    using YamlDotNet.Serialization;

    public class CreateConfig
    {
        [YamlMember(Alias = "include-footer")]
        public bool IncludeFooter { get; set; }

        [YamlMember(Alias = "footer-heading")]
        public string FooterHeading { get; set; }

        [YamlMember(Alias = "footer-content")]
        public string FooterContent { get; set; }

        [YamlMember(Alias = "footer-includes-milestone")]
        public bool FooterIncludesMilestone { get; set; }

        [YamlMember(Alias = "milestone-replace-text")]
        public string MilestoneReplaceText { get; set; }
    }
}