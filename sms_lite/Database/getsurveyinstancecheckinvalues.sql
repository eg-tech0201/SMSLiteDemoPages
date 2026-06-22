DROP PROCEDURE IF EXISTS getsurveyinstancecheckinvalues;

DELIMITER $$

CREATE PROCEDURE getsurveyinstancecheckinvalues(
    IN filter_survey_id INT,
    IN filter_survey_date DATE,
    IN filter_sample_id INT
)
BEGIN
    /*
      Direct chart source from sms.checkin.

      A selected survey instance is filtered directly by:
        sms.checkin.survey_id
        sms.checkin.survey_date
        sms.checkin.sample_id

      No DISTINCT, GROUP BY, ranking, or collapsing by sms.checkin.skey is
      performed. Every matching sms.checkin row is returned.

      Chart variable sources:
        received/not received: sms.checkin.respdate
        response status:       sms.checkin.response_code
        received mode:         sms.checkin.data_capture_code
        collection status:     sms.checkin.dcms_code_id
    */
    SELECT
        sms.checkin.respdate AS respdate,
        sms.checkin.response_code AS response_code,
        sms.checkin.data_capture_code AS data_capture_code,
        sms.checkin.dcms_code_id AS dcms_code_id
    FROM sms.checkin
    WHERE sms.checkin.survey_id = filter_survey_id
      AND sms.checkin.survey_date = filter_survey_date
      AND sms.checkin.sample_id = filter_sample_id;
END$$

DELIMITER ;
