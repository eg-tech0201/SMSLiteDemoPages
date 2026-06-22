DROP PROCEDURE IF EXISTS getsurveyinstances;

DELIMITER $$

CREATE PROCEDURE getsurveyinstances(
    IN row_limit INT,
    IN row_offset INT,
    IN filter_survey_id INT,
    IN filter_sample_id INT,
    IN filter_survey_search VARCHAR(200),
    IN filter_survey_date DATE,
    IN filter_start_date DATE,
    IN filter_stop_date DATE,
    IN filter_mail_flag SMALLINT,
    IN filter_cawi_flag SMALLINT,
    IN filter_cati_flag SMALLINT,
    IN filter_capi_flag SMALLINT,
    IN filter_hq_review_flag SMALLINT
)
BEGIN
    /*
      Direct source mapping:

      sms.survey_detail is joined to sms.survey_master by:
        sms.survey_detail.survey_id = sms.survey_master.survey_id

      sms.survey_master is joined to sms.sample by:
        sms.survey_master.sample_id = sms.sample.sample_id

      sms.survey_master is joined to sms.frequency_codes by:
        sms.survey_master.frequency_code = sms.frequency_codes.frequency_code

      sms.survey_master is joined to sms.omb by:
        sms.survey_master.omb_number = sms.omb.omb_number

      No DISTINCT, GROUP BY, ranking, minimum, maximum, or calculated survey
      values are used. Every matching sms.survey_detail row is returned.

      COUNT(*) OVER() is only the total number of filtered grid rows used by
      server-side pagination. filter_survey_search is the Surveys search input.
    */
    SELECT
        sms.survey_detail.survey_id AS survey_id,
        sms.survey_detail.survey_date AS survey_date,
        sms.survey_master.sample_id AS sample_id,
        sms.survey_detail.period_id AS period_id,
        sms.survey_detail.active_flag AS active_flag,
        sms.survey_detail.mail_start_date AS mail_start_date,
        sms.survey_detail.mail_stop_date AS mail_stop_date,
        sms.survey_detail.cawi_start_date AS cawi_start_date,
        sms.survey_detail.cawi_stop_date AS cawi_stop_date,
        sms.survey_detail.capi_start_date AS capi_start_date,
        sms.survey_detail.capi_stop_date AS capi_stop_date,
        sms.survey_detail.cati_start_date AS cati_start_date,
        sms.survey_detail.cati_stop_date AS cati_stop_date,
        sms.survey_detail.cati_app_indicator AS cati_app,
        sms.survey_detail.start_date AS start_date,
        sms.survey_detail.stop_date AS stop_date,
        sms.survey_detail.sample_month AS sample_month,
        sms.survey_detail.hq_review_flag AS hq_review_flag,
        sms.survey_detail.populated_rows AS populated_rows,
        sms.survey_master.mail_flag AS mail_flag,
        sms.survey_master.cawi_flag AS cawi_flag,
        sms.survey_master.cati_flag AS cati_flag,
        sms.survey_master.capi_flag AS capi_flag,
        sms.survey_master.survey_title AS survey_title,
        sms.survey_master.survey_subtitle AS survey_subtitle,
        sms.survey_master.frequency_code AS frequency_code,
        sms.frequency_codes.frequency_desc AS frequency_desc,
        sms.survey_master.omb_number AS omb_number,
        sms.omb.omb_expires AS omb_expires,
        sms.survey_master.base_month AS base_month,
        sms.survey_master.project_code AS project_code,
        sms.survey_master.marked_version AS marked_version,
        sms.sample.sample_name AS sample_name,
        sms.sample.surveyID AS elmo_survey_id,
        sms.survey_detail.period_id AS elmo_period_id,
        sms.survey_detail.sample_month AS elmo_month,
        COUNT(*) OVER() AS total_row_count
    FROM sms.survey_detail
    INNER JOIN sms.survey_master
        ON sms.survey_master.survey_id = sms.survey_detail.survey_id
    LEFT JOIN sms.frequency_codes
        ON sms.frequency_codes.frequency_code = sms.survey_master.frequency_code
    LEFT JOIN sms.omb
        ON sms.omb.omb_number = sms.survey_master.omb_number
    LEFT JOIN sms.sample
        ON sms.sample.sample_id = sms.survey_master.sample_id
    WHERE (
            filter_survey_search IS NULL
            OR sms.survey_master.survey_title LIKE CONCAT('%', filter_survey_search, '%')
            OR CAST(sms.survey_detail.survey_id AS CHAR) LIKE CONCAT('%', filter_survey_search, '%')
        )
      AND (filter_survey_id IS NULL OR sms.survey_detail.survey_id = filter_survey_id)
      AND (filter_sample_id IS NULL OR sms.survey_master.sample_id = filter_sample_id)
      AND (filter_survey_date IS NULL OR sms.survey_detail.survey_date = filter_survey_date)
      AND (filter_start_date IS NULL OR sms.survey_detail.start_date >= filter_start_date)
      AND (filter_stop_date IS NULL OR sms.survey_detail.stop_date <= filter_stop_date)
      AND (filter_mail_flag IS NULL OR sms.survey_master.mail_flag = filter_mail_flag)
      AND (filter_cawi_flag IS NULL OR sms.survey_master.cawi_flag = filter_cawi_flag)
      AND (filter_cati_flag IS NULL OR sms.survey_master.cati_flag = filter_cati_flag)
      AND (filter_capi_flag IS NULL OR sms.survey_master.capi_flag = filter_capi_flag)
      AND (filter_hq_review_flag IS NULL OR sms.survey_detail.hq_review_flag = filter_hq_review_flag)
    ORDER BY
        sms.survey_detail.survey_date DESC,
        sms.survey_detail.survey_id,
        sms.survey_master.sample_id
    LIMIT row_limit OFFSET row_offset;
END$$

DELIMITER ;
