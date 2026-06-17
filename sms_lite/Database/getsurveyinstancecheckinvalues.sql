DROP PROCEDURE IF EXISTS getsurveyinstancecheckinvalues;

DELIMITER $$

CREATE PROCEDURE getsurveyinstancecheckinvalues(
    IN filter_survey_id INT,
    IN filter_survey_date DATETIME
)
BEGIN
    /*
      Detail/chart source only.

      This can return many rows, so call it only after a specific survey
      instance is opened. It intentionally does not feed the top-level
      Surveys grid.

      response_code is used for the Complete / Refusal / Inaccessible
      chart slices.
    */
    SELECT
        checkin.respdate AS respdate,
        checkin.response_code AS response_code,
        checkin.data_capture_code AS data_capture_code,
        checkin.dcms_code_id AS dcms_code_id
    FROM sms.checkin AS checkin
    WHERE checkin.survey_id = filter_survey_id
      AND DATE(checkin.survey_date) = DATE(filter_survey_date);
END$$

DELIMITER ;
