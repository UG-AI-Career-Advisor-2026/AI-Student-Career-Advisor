# Career Catalogue Documentation

## Overview
This document describes the data structure and sources for the AI Student Career Advisor's career catalogue. The catalogue contains a curated, static list of entry-to-mid-level technology roles used to power recommendations, skill-gap analyses, and learning roadmaps.

## Data Format
The data is stored in `data/career-catalog.json` as a JSON array. Each object contains:
- `code`: A unique string identifier.
- `name`: The official title of the career.
- `description`: A brief summary of the role's responsibilities.
- `requiredSkills`: An array of at least six core technical and soft skills.
- `recommendedCertifications`: Industry-recognized professional certifications.
- `suggestedLearningTopics`: Key areas for continued professional development.

## Included Careers
1. Software Developer
2. Data Analyst
3. Cybersecurity Analyst
4. Cloud Engineer
5. Network Administrator
6. Database Administrator
7. UI/UX Designer
8. AI/ML Engineer

## Sources
The skills, certifications, and learning topics were compiled using official certification bodies and industry-standard occupational databases. No URLs or certifications have been fabricated.

### Occupational Databases
- **U.S. Bureau of Labor Statistics (BLS) Occupational Outlook Handbook**: [https://www.bls.gov/ooh/](https://www.bls.gov/ooh/)
- **O*NET OnLine**: [https://www.onetonline.org/](https://www.onetonline.org/)

### Certification Providers (Official Sources)
- **AWS Training and Certification**: [https://aws.amazon.com/training/](https://aws.amazon.com/training/)
- **Microsoft Learn Certifications**: [https://learn.microsoft.com/en-us/certifications/](https://learn.microsoft.com/en-us/certifications/)
- **Google Career Certificates**: [https://grow.google/certificates/](https://grow.google/certificates/)
- **CompTIA**: [https://www.comptia.org/certifications](https://www.comptia.org/certifications)
- **Cisco Learning Network**: [https://learningnetwork.cisco.com/s/certifications](https://learningnetwork.cisco.com/s/certifications)
- **Oracle University**: [https://education.oracle.com/](https://education.oracle.com/)
- **Nielsen Norman Group (NN/g)**: [https://www.nngroup.com/training/](https://www.nngroup.com/training/)