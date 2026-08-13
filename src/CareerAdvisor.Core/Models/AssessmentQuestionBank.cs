namespace CareerAdvisor.Core.Models;

/// <summary>
/// Provides a predefined set of assessment questions for career and skill assessment.
/// Questions are categorized into Interests, Skills, and WorkPreferences.
/// Each question has a unique code and multiple choice options.
/// </summary>
public static class AssessmentQuestionBank
{
    /// <summary>
    /// Gets all assessment questions for the career assessment.
    /// Contains 15 questions covering interests, skills, and work preferences.
    /// </summary>
    public static List<AssessmentQuestion> GetAllQuestions()
    {
        return new List<AssessmentQuestion>
        {
            // ========== INTERESTS QUESTIONS (Q1-Q5) ==========
            CreateInterestsQuestion1(),
            CreateInterestsQuestion2(),
            CreateInterestsQuestion3(),
            CreateInterestsQuestion4(),
            CreateInterestsQuestion5(),

            // ========== SKILLS QUESTIONS (Q6-Q10) ==========
            CreateSkillsQuestion6(),
            CreateSkillsQuestion7(),
            CreateSkillsQuestion8(),
            CreateSkillsQuestion9(),
            CreateSkillsQuestion10(),

            // ========== WORK PREFERENCES QUESTIONS (Q11-Q15) ==========
            CreateWorkPreferencesQuestion11(),
            CreateWorkPreferencesQuestion12(),
            CreateWorkPreferencesQuestion13(),
            CreateWorkPreferencesQuestion14(),
            CreateWorkPreferencesQuestion15()
        };
    }

    // ========== INTERESTS QUESTIONS ==========

    private static AssessmentQuestion CreateInterestsQuestion1()
    {
        return new AssessmentQuestion
        {
            Id = new Guid("11111111-1111-1111-1111-111111111111"),
            Code = "Q1_INT_TECH",
            Text = "How interested are you in technology and software development?",
            Category = "Interests",
            IsRequired = true,
            DisplayOrder = 1,
            Options = new List<AssessmentOption>
            {
                new AssessmentOption { Id = new Guid("11110001-1111-1111-1111-111111111111"), Code = "Q1_OPT_A", Value = "Very interested - love learning new technologies", Description = "Passionate about tech innovation" },
                new AssessmentOption { Id = new Guid("11110002-1111-1111-1111-111111111111"), Code = "Q1_OPT_B", Value = "Somewhat interested - open to tech roles", Description = "Willing to work with technology" },
                new AssessmentOption { Id = new Guid("11110003-1111-1111-1111-111111111111"), Code = "Q1_OPT_C", Value = "Neutral - could go either way", Description = "Undecided about tech focus" },
                new AssessmentOption { Id = new Guid("11110004-1111-1111-1111-111111111111"), Code = "Q1_OPT_D", Value = "Not interested - prefer other fields", Description = "More interested in non-tech careers" }
            }
        };
    }

    private static AssessmentQuestion CreateInterestsQuestion2()
    {
        return new AssessmentQuestion
        {
            Id = new Guid("22222222-2222-2222-2222-222222222222"),
            Code = "Q2_INT_DATA",
            Text = "What is your interest level in data analysis and business intelligence?",
            Category = "Interests",
            IsRequired = true,
            DisplayOrder = 2,
            Options = new List<AssessmentOption>
            {
                new AssessmentOption { Id = new Guid("22220001-2222-2222-2222-222222222222"), Code = "Q2_OPT_A", Value = "Very interested - enjoy working with data", Description = "Strong interest in analytics" },
                new AssessmentOption { Id = new Guid("22220002-2222-2222-2222-222222222222"), Code = "Q2_OPT_B", Value = "Somewhat interested - curious about insights", Description = "Moderate interest in data" },
                new AssessmentOption { Id = new Guid("22220003-2222-2222-2222-222222222222"), Code = "Q2_OPT_C", Value = "Neutral - no strong preference", Description = "Undecided about data roles" },
                new AssessmentOption { Id = new Guid("22220004-2222-2222-2222-222222222222"), Code = "Q2_OPT_D", Value = "Not interested - prefer creative work", Description = "Prefer non-analytical roles" }
            }
        };
    }

    private static AssessmentQuestion CreateInterestsQuestion3()
    {
        return new AssessmentQuestion
        {
            Id = new Guid("33333333-3333-3333-3333-333333333333"),
            Code = "Q3_INT_CREA",
            Text = "Are you interested in creative and design-focused work?",
            Category = "Interests",
            IsRequired = true,
            DisplayOrder = 3,
            Options = new List<AssessmentOption>
            {
                new AssessmentOption { Id = new Guid("33330001-3333-3333-3333-333333333333"), Code = "Q3_OPT_A", Value = "Very interested - love creative expression", Description = "Passionate about design and creativity" },
                new AssessmentOption { Id = new Guid("33330002-3333-3333-3333-333333333333"), Code = "Q3_OPT_B", Value = "Somewhat interested - enjoy some creative aspects", Description = "Open to creative opportunities" },
                new AssessmentOption { Id = new Guid("33330003-3333-3333-3333-333333333333"), Code = "Q3_OPT_C", Value = "Neutral - balanced interest in creative and analytical", Description = "Equal interest in both" },
                new AssessmentOption { Id = new Guid("33330004-3333-3333-3333-333333333333"), Code = "Q3_OPT_D", Value = "Not interested - prefer logical problem-solving", Description = "More analytical than creative" }
            }
        };
    }

    private static AssessmentQuestion CreateInterestsQuestion4()
    {
        return new AssessmentQuestion
        {
            Id = new Guid("44444444-4444-4444-4444-444444444444"),
            Code = "Q4_INT_MGMT",
            Text = "How interested are you in leadership and management roles?",
            Category = "Interests",
            IsRequired = true,
            DisplayOrder = 4,
            Options = new List<AssessmentOption>
            {
                new AssessmentOption { Id = new Guid("44440001-4444-4444-4444-444444444444"), Code = "Q4_OPT_A", Value = "Very interested - aspire to leadership positions", Description = "Strong interest in management" },
                new AssessmentOption { Id = new Guid("44440002-4444-4444-4444-444444444444"), Code = "Q4_OPT_B", Value = "Somewhat interested - open to management opportunities", Description = "Willing to pursue leadership" },
                new AssessmentOption { Id = new Guid("44440003-4444-4444-4444-444444444444"), Code = "Q4_OPT_C", Value = "Neutral - prefer technical expertise over management", Description = "Prefer technical depth" },
                new AssessmentOption { Id = new Guid("44440004-4444-4444-4444-444444444444"), Code = "Q4_OPT_D", Value = "Not interested - prefer individual contributor roles", Description = "Strong preference for hands-on work" }
            }
        };
    }

    private static AssessmentQuestion CreateInterestsQuestion5()
    {
        return new AssessmentQuestion
        {
            Id = new Guid("55555555-5555-5555-5555-555555555555"),
            Code = "Q5_INT_SOCIAL",
            Text = "What is your interest in customer-facing or social impact work?",
            Category = "Interests",
            IsRequired = true,
            DisplayOrder = 5,
            Options = new List<AssessmentOption>
            {
                new AssessmentOption { Id = new Guid("55550001-5555-5555-5555-555555555555"), Code = "Q5_OPT_A", Value = "Very interested - want to help people and create impact", Description = "Passionate about social impact" },
                new AssessmentOption { Id = new Guid("55550002-5555-5555-5555-555555555555"), Code = "Q5_OPT_B", Value = "Somewhat interested - enjoy customer interaction", Description = "Open to customer-facing roles" },
                new AssessmentOption { Id = new Guid("55550003-5555-5555-5555-555555555555"), Code = "Q5_OPT_C", Value = "Neutral - balanced between technical and people focus", Description = "No strong preference" },
                new AssessmentOption { Id = new Guid("55550004-5555-5555-5555-555555555555"), Code = "Q5_OPT_D", Value = "Not interested - prefer backend/infrastructure work", Description = "Prefer non-customer-facing roles" }
            }
        };
    }

    // ========== SKILLS QUESTIONS ==========

    private static AssessmentQuestion CreateSkillsQuestion6()
    {
        return new AssessmentQuestion
        {
            Id = new Guid("66666666-6666-6666-6666-666666666666"),
            Code = "Q6_SKILL_PROG",
            Text = "What is your proficiency level with programming and coding?",
            Category = "Skills",
            IsRequired = true,
            DisplayOrder = 6,
            Options = new List<AssessmentOption>
            {
                new AssessmentOption { Id = new Guid("66660001-6666-6666-6666-666666666666"), Code = "Q6_OPT_A", Value = "Expert - extensive experience with multiple languages", Description = "Advanced programming skills" },
                new AssessmentOption { Id = new Guid("66660002-6666-6666-6666-666666666666"), Code = "Q6_OPT_B", Value = "Intermediate - can build applications independently", Description = "Solid programming knowledge" },
                new AssessmentOption { Id = new Guid("66660003-6666-6666-6666-666666666666"), Code = "Q6_OPT_C", Value = "Beginner - learning and comfortable with basics", Description = "Basic programming skills" },
                new AssessmentOption { Id = new Guid("66660004-6666-6666-6666-666666666666"), Code = "Q6_OPT_D", Value = "None - no programming experience", Description = "No coding background" }
            }
        };
    }

    private static AssessmentQuestion CreateSkillsQuestion7()
    {
        return new AssessmentQuestion
        {
            Id = new Guid("77777777-7777-7777-7777-777777777777"),
            Code = "Q7_SKILL_COMM",
            Text = "How would you rate your communication and presentation skills?",
            Category = "Skills",
            IsRequired = true,
            DisplayOrder = 7,
            Options = new List<AssessmentOption>
            {
                new AssessmentOption { Id = new Guid("77770001-7777-7777-7777-777777777777"), Code = "Q7_OPT_A", Value = "Excellent - confident communicator, strong presenter", Description = "Strong communication abilities" },
                new AssessmentOption { Id = new Guid("77770002-7777-7777-7777-777777777777"), Code = "Q7_OPT_B", Value = "Good - can communicate clearly in most situations", Description = "Solid communication skills" },
                new AssessmentOption { Id = new Guid("77770003-7777-7777-7777-777777777777"), Code = "Q7_OPT_C", Value = "Fair - some anxiety with presentations", Description = "Developing communication skills" },
                new AssessmentOption { Id = new Guid("77770004-7777-7777-7777-777777777777"), Code = "Q7_OPT_D", Value = "Poor - prefer written communication", Description = "Prefer non-verbal communication" }
            }
        };
    }

    private static AssessmentQuestion CreateSkillsQuestion8()
    {
        return new AssessmentQuestion
        {
            Id = new Guid("88888888-8888-8888-8888-888888888888"),
            Code = "Q8_SKILL_PROB",
            Text = "What is your problem-solving and critical thinking ability?",
            Category = "Skills",
            IsRequired = true,
            DisplayOrder = 8,
            Options = new List<AssessmentOption>
            {
                new AssessmentOption { Id = new Guid("88880001-8888-8888-8888-888888888888"), Code = "Q8_OPT_A", Value = "Excellent - naturally good at analyzing complex problems", Description = "Strong problem-solver" },
                new AssessmentOption { Id = new Guid("88880002-8888-8888-8888-888888888888"), Code = "Q8_OPT_B", Value = "Good - can handle most problems with some guidance", Description = "Solid analytical skills" },
                new AssessmentOption { Id = new Guid("88880003-8888-8888-8888-888888888888"), Code = "Q8_OPT_C", Value = "Fair - need time to work through complex issues", Description = "Developing problem-solving skills" },
                new AssessmentOption { Id = new Guid("88880004-8888-8888-8888-888888888888"), Code = "Q8_OPT_D", Value = "Prefer structured solutions and guidance", Description = "Prefer clear direction" }
            }
        };
    }

    private static AssessmentQuestion CreateSkillsQuestion9()
    {
        return new AssessmentQuestion
        {
            Id = new Guid("99999999-9999-9999-9999-999999999999"),
            Code = "Q9_SKILL_COLL",
            Text = "How strong are your teamwork and collaboration skills?",
            Category = "Skills",
            IsRequired = true,
            DisplayOrder = 9,
            Options = new List<AssessmentOption>
            {
                new AssessmentOption { Id = new Guid("99990001-9999-9999-9999-999999999999"), Code = "Q9_OPT_A", Value = "Excellent - natural team player, enjoy collaboration", Description = "Strong teamwork abilities" },
                new AssessmentOption { Id = new Guid("99990002-9999-9999-9999-999999999999"), Code = "Q9_OPT_B", Value = "Good - work well with others in most situations", Description = "Solid collaboration skills" },
                new AssessmentOption { Id = new Guid("99990003-9999-9999-9999-999999999999"), Code = "Q9_OPT_C", Value = "Fair - prefer working independently but can collaborate", Description = "Developing teamwork skills" },
                new AssessmentOption { Id = new Guid("99990004-9999-9999-9999-999999999999"), Code = "Q9_OPT_D", Value = "Prefer working alone on focused tasks", Description = "Strong individual contributor" }
            }
        };
    }

    private static AssessmentQuestion CreateSkillsQuestion10()
    {
        return new AssessmentQuestion
        {
            Id = new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Code = "Q10_SKILL_LEARN",
            Text = "How do you rate your ability to learn new skills quickly?",
            Category = "Skills",
            IsRequired = true,
            DisplayOrder = 10,
            Options = new List<AssessmentOption>
            {
                new AssessmentOption { Id = new Guid("aaaa0001-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), Code = "Q10_OPT_A", Value = "Very high - quickly master new technologies and concepts", Description = "Fast learner" },
                new AssessmentOption { Id = new Guid("aaaa0002-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), Code = "Q10_OPT_B", Value = "High - can pick up new skills with practice", Description = "Good learner" },
                new AssessmentOption { Id = new Guid("aaaa0003-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), Code = "Q10_OPT_C", Value = "Moderate - need time and structured training", Description = "Steady learner" },
                new AssessmentOption { Id = new Guid("aaaa0004-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), Code = "Q10_OPT_D", Value = "Prefer deep expertise in current skills", Description = "Prefer specialization" }
            }
        };
    }

    // ========== WORK PREFERENCES QUESTIONS ==========

    private static AssessmentQuestion CreateWorkPreferencesQuestion11()
    {
        return new AssessmentQuestion
        {
            Id = new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            Code = "Q11_WORK_ENV",
            Text = "What work environment do you prefer?",
            Category = "WorkPreferences",
            IsRequired = true,
            DisplayOrder = 11,
            Options = new List<AssessmentOption>
            {
                new AssessmentOption { Id = new Guid("bbbb0001-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), Code = "Q11_OPT_A", Value = "Remote/Hybrid - work from home flexibility", Description = "Prefer remote work" },
                new AssessmentOption { Id = new Guid("bbbb0002-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), Code = "Q11_OPT_B", Value = "Office-based - collaborative in-person environment", Description = "Prefer office setting" },
                new AssessmentOption { Id = new Guid("bbbb0003-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), Code = "Q11_OPT_C", Value = "Flexible - mix of remote and office depending on needs", Description = "Open to flexible arrangements" },
                new AssessmentOption { Id = new Guid("bbbb0004-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), Code = "Q11_OPT_D", Value = "No preference - either is fine", Description = "Environment agnostic" }
            }
        };
    }

    private static AssessmentQuestion CreateWorkPreferencesQuestion12()
    {
        return new AssessmentQuestion
        {
            Id = new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            Code = "Q12_WORK_PACE",
            Text = "What pace of work do you prefer?",
            Category = "WorkPreferences",
            IsRequired = true,
            DisplayOrder = 12,
            Options = new List<AssessmentOption>
            {
                new AssessmentOption { Id = new Guid("cccc0001-cccc-cccc-cccc-cccccccccccc"), Code = "Q12_OPT_A", Value = "Fast-paced - startup/high-growth environment", Description = "Thrive in fast pace" },
                new AssessmentOption { Id = new Guid("cccc0002-cccc-cccc-cccc-cccccccccccc"), Code = "Q12_OPT_B", Value = "Moderate - balanced workload with clear priorities", Description = "Prefer sustainable pace" },
                new AssessmentOption { Id = new Guid("cccc0003-cccc-cccc-cccc-cccccccccccc"), Code = "Q12_OPT_C", Value = "Flexible - adapt to project demands", Description = "Flexible approach" },
                new AssessmentOption { Id = new Guid("cccc0004-cccc-cccc-cccc-cccccccccccc"), Code = "Q12_OPT_D", Value = "Slow-paced - time for thorough work", Description = "Prefer careful planning" }
            }
        };
    }

    private static AssessmentQuestion CreateWorkPreferencesQuestion13()
    {
        return new AssessmentQuestion
        {
            Id = new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"),
            Code = "Q13_WORK_STABIL",
            Text = "What is more important to you - stability or growth?",
            Category = "WorkPreferences",
            IsRequired = true,
            DisplayOrder = 13,
            Options = new List<AssessmentOption>
            {
                new AssessmentOption { Id = new Guid("dddd0001-dddd-dddd-dddd-dddddddddddd"), Code = "Q13_OPT_A", Value = "Growth - career advancement and learning opportunities", Description = "Prioritize growth" },
                new AssessmentOption { Id = new Guid("dddd0002-dddd-dddd-dddd-dddddddddddd"), Code = "Q13_OPT_B", Value = "Stability - secure job with consistent income", Description = "Value stability" },
                new AssessmentOption { Id = new Guid("dddd0003-dddd-dddd-dddd-dddddddddddd"), Code = "Q13_OPT_C", Value = "Balanced - want both growth and security", Description = "Both equally important" },
                new AssessmentOption { Id = new Guid("dddd0004-dddd-dddd-dddd-dddddddddddd"), Code = "Q13_OPT_D", Value = "Depends on life stage and circumstances", Description = "Situational preference" }
            }
        };
    }

    private static AssessmentQuestion CreateWorkPreferencesQuestion14()
    {
        return new AssessmentQuestion
        {
            Id = new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
            Code = "Q14_WORK_COMP",
            Text = "What compensation model interests you most?",
            Category = "WorkPreferences",
            IsRequired = true,
            DisplayOrder = 14,
            Options = new List<AssessmentOption>
            {
                new AssessmentOption { Id = new Guid("eeee0001-eeee-eeee-eeee-eeeeeeeeeeee"), Code = "Q14_OPT_A", Value = "Salary with benefits - traditional employment", Description = "Traditional employment" },
                new AssessmentOption { Id = new Guid("eeee0002-eeee-eeee-eeee-eeeeeeeeeeee"), Code = "Q14_OPT_B", Value = "Salary + equity/stock options - growth potential", Description = "Growth-oriented comp" },
                new AssessmentOption { Id = new Guid("eeee0003-eeee-eeee-eeee-eeeeeeeeeeee"), Code = "Q14_OPT_C", Value = "Freelance/contract - flexibility and independence", Description = "Independent contracting" },
                new AssessmentOption { Id = new Guid("eeee0004-eeee-eeee-eeee-eeeeeeeeeeee"), Code = "Q14_OPT_D", Value = "No strong preference - depends on role fit", Description = "Role-dependent" }
            }
        };
    }

    private static AssessmentQuestion CreateWorkPreferencesQuestion15()
    {
        return new AssessmentQuestion
        {
            Id = new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff"),
            Code = "Q15_WORK_INDUS",
            Text = "What industry sector interests you most?",
            Category = "WorkPreferences",
            IsRequired = true,
            DisplayOrder = 15,
            Options = new List<AssessmentOption>
            {
                new AssessmentOption { Id = new Guid("ffff0001-ffff-ffff-ffff-ffffffffffff"), Code = "Q15_OPT_A", Value = "Technology/SaaS - software and digital innovation", Description = "Tech industry" },
                new AssessmentOption { Id = new Guid("ffff0002-ffff-ffff-ffff-ffffffffffff"), Code = "Q15_OPT_B", Value = "Finance/FinTech - banking and financial services", Description = "Finance sector" },
                new AssessmentOption { Id = new Guid("ffff0003-ffff-ffff-ffff-ffffffffffff"), Code = "Q15_OPT_C", Value = "Healthcare/Biotech - medical and health innovation", Description = "Healthcare sector" },
                new AssessmentOption { Id = new Guid("ffff0004-ffff-ffff-ffff-ffffffffffff"), Code = "Q15_OPT_D", Value = "Other - different industry focus", Description = "Other sectors" }
            }
        };
    }
}
