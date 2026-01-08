Feature: CheckBoxList selection behavior
  A comprehensive test suite validating all selection logic for the CheckBoxList component.

  Background:
    Given a new CheckBoxList component

  @basic
  Scenario: Adding a selection
    When I toggle value "101" with text "John Doe" as checked
    Then the selected values should contain "101"
    And the selected texts should contain "John Doe"

  @basic
  Scenario: Removing a selection
    Given a CheckBoxList component with value "101" and text "John Doe" selected
    When I toggle value "101" with text "John Doe" as unchecked
    Then the selected values should not contain "101"
    And the selected texts should not contain "John Doe"

  @multiple
  Scenario: Adding multiple selections
    When I toggle value "1" with text "A" as checked
    And I toggle value "2" with text "B" as checked
    And I toggle value "3" with text "C" as checked
    Then the selected values should contain "1"
    And the selected values should contain "2"
    And the selected values should contain "3"
    And the selected texts should contain "A"
    And the selected texts should contain "B"
    And the selected texts should contain "C"

  @multiple
  Scenario: Removing one value from multiple selections
    Given a CheckBoxList component with values "1,2,3" and texts "A,B,C" selected
    When I toggle value "2" with text "B" as unchecked
    Then the selected values should contain "1"
    And the selected values should not contain "2"
    And the selected values should contain "3"

  @duplicates
  Scenario: Preventing duplicates
    Given a CheckBoxList component with value "Blue" and text "Blue" selected
    When I toggle value "Blue" with text "Blue" as checked
    Then the selected values count should be 1
    And the selected texts count should be 1

  @outline
  Scenario Outline: Toggling values repeatedly
    When I toggle value "<value>" with text "<text>" as checked
    And I toggle value "<value>" with text "<text>" as unchecked
    And I toggle value "<value>" with text "<text>" as checked
    Then the selected values should contain "<value>"
    And the selected texts should contain "<text>"

    Examples:
      | value | text |
      | X     | X    |
      | 10    | Ten  |
      | 999   | Code |

  @edge
  Scenario: Handling empty values
    When I toggle value "" with text "" as checked
    Then the selected values should contain ""
    And the selected texts should contain ""

  @edge
  Scenario: Handling null values
    When I toggle value "<null>" with text "<null>" as checked
    Then the selected values should contain "<null>"
    And the selected texts should contain "<null>"

  @order
  Scenario: Order is preserved
    When I toggle value "A" with text "A" as checked
    And I toggle value "B" with text "B" as checked
    And I toggle value "C" with text "C" as checked
    Then the selected values should be in order "A,B,C"

  @datatable
  Scenario: Adding multiple values using a table
    When I toggle the following values:
      | value | text |
      | 1     | One  |
      | 2     | Two  |
      | 3     | Three |
    Then the selected values should contain "1"
    And the selected values should contain "2"
    And the selected values should contain "3"
