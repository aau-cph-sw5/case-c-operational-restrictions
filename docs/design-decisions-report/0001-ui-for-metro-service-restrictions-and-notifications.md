# DDR 0001. UI for Metro Service Restrictions and Notifications

**Status:** Proposed feedback from metro service needed

**Date:** 2026-10-08

**Deciders:** Group 4, Artem Shevchenko and Joakim Uldal Boldt  

**References:** "restriction-form-and-notification-vertical-slice.png", "restriction-form-screen.png", "notification-screen.png", "[Preview](https://www.figma.com/proto/ZWMLi7qSY9twx3yPhS6osY/Restriciton-Form-Vertical-Slice?node-id=15-122&p=f&t=CmTKHKzFbqn69ISR-1&scaling=scale-down&content-scaling=fixed&page-id=0%3A1&starting-point-node-id=15%3A122
)", "[Design file](https://www.figma.com/design/ZWMLi7qSY9twx3yPhS6osY/Restriciton-Form-Vertical-Slice?node-id=0-1&t=Wti9VgUMuykHcE5s-1)"

## Context

We are designing the restriction form and notification screens for an internal Metro application. The interface must align with the provided Metro documentation for form fields.

The notification screen must clearly and concisly communicate pending actions, so that the users are informed of any missed processes that need to be handeld. 



To back design decision, we use the [10 Usability Heuristics for User Interface Design](`https://www.nngroup.com/articles/ten-usability-heuristics/`) from Nielsen Norman Group.

## Design Principles & Heuristics Applied

* **Brand Identity:** During stakeholder review, we discovered additional information about the brand identity and color palette. That said, as Metro changes owners, the original (`metroservice.dk`) orange color palette is unwanted; instead, a generic, widely used color palette is preferred for ease of changing once the new owners take over.

* **Required Fields:** During stakeholder review, we discovered which fields were required for creation of the operational restriction like the description and which fields were optinal such as the end date of the restriciton, and implemented a red star on the required fields.

* **Attachment system:** Added an additional attachment section inside the restriction form, to simplify referencing, and digitize any additional information that applies to a restriction.

* **Nielsen #1 (Visibility of System Status):** Utilized dynamic visual tags, relative timestamps (e.g., "10 min. siden"), clear action buttons like "Underskrive nu", tinted unread notification boxes,  and an unread counter in the top header.


* **Nielsen #4 (Consistency and Standards):** Used standard mobile UI patterns, such as standard icon sets, segmented top tabs, and a bottom navigation bar highlighting the current location.


* **Nielsen #6 (Recognition Rather Than Recall):** Added category icons (Warning for pending Driftsrestriktion signatures, Pen for requested changes, info symbol for System Info) to the left side of each notification card to provide easily digestible information, without requiring heavy reading.


* **Nielsen #7 (Flexibility and Efficiency of Use):** Introduced top filter buttons ("Alle", "Ulæste", "Påkrævet") to allow users to filter items rather than manually scrolling.



## Decision

We will implement the restriction form and notification views using a monochromatic color palette, structured with standardized mobile UI components, category icons, and segmented filtering tabs to prioritize and highlight important items.
