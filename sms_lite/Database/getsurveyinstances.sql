DROP PROCEDURE IF EXISTS getsurveyinstances;

DELIMITER $$

CREATE PROCEDURE getsurveyinstances(
    IN row_limit INT,
    IN row_offset INT,
    IN filter_survey_id INT,
    IN filter_sample_id INT,
    IN filter_survey_search VARCHAR(200),
    IN filter_survey_date DATETIME,
    IN filter_start_date DATETIME,
    IN filter_stop_date DATETIME,
    IN filter_mail_flag SMALLINT,
    IN filter_cawi_flag SMALLINT,
    IN filter_cati_flag SMALLINT,
    IN filter_capi_flag SMALLINT,
    IN filter_hq_review_flag SMALLINT
)
BEGIN
    /*
      Top-level Surveys grid source.

      Keep this procedure at the survey-instance grain:
        survey_id + survey_date

      Do not join respondent-level tables here. Use separate detail/summary
      procedures for sample_detail_data and checkin once a user opens a
      specific survey instance.
    */
    WITH filtered_survey_instances AS (
        SELECT
            DISTINCT
            survey_master.sample_id AS sample_id,
            survey_detail.survey_id,
            survey_detail.survey_date,
            survey_detail.period_id,
            survey_detail.active_flag,
            survey_detail.mail_start_date,
            survey_detail.mail_stop_date,
            survey_detail.cawi_start_date,
            survey_detail.cawi_stop_date,
            survey_detail.capi_start_date,
            survey_detail.capi_stop_date,
            survey_detail.cati_start_date,
            survey_detail.cati_stop_date,
            survey_detail.cati_app_indicator AS cati_app,
            survey_detail.start_date,
            survey_detail.stop_date,
            survey_detail.sample_month,
            survey_detail.hq_review_flag,
            survey_detail.populated_rows,

            survey_master.mail_flag AS mail_flag,
            survey_master.cawi_flag AS cawi_flag,
            survey_master.cati_flag AS cati_flag,
            survey_master.capi_flag AS capi_flag,
            survey_master.survey_title AS survey_title,
            survey_master.survey_subtitle AS survey_subtitle,
            survey_master.frequency_code AS frequency_code,
            frequency_codes.frequency_desc AS frequency_desc,
            survey_master.omb_number AS omb_number,
            omb.omb_expires AS omb_expires,
            survey_master.base_month AS base_month,
            survey_master.project_code AS project_code,
            survey_master.marked_version AS marked_version,

            sample.sample_name AS sample_name,
            sample.surveyID AS elmo_survey_id,
            survey_detail.period_id AS elmo_period_id,
            survey_detail.sample_month AS elmo_month
        FROM sms.survey_detail AS survey_detail
        INNER JOIN sms.survey_master AS survey_master
            ON survey_master.survey_id = survey_detail.survey_id
        LEFT JOIN sms.frequency_codes AS frequency_codes
            ON frequency_codes.frequency_code = survey_master.frequency_code
        LEFT JOIN sms.omb AS omb
            ON omb.omb_number = survey_master.omb_number
        LEFT JOIN sms.sample AS sample
            ON sample.sample_id = survey_master.sample_id
        WHERE (filter_survey_search IS NULL
                OR survey_master.survey_title LIKE CONCAT('%', filter_survey_search, '%')
                OR CAST(survey_detail.survey_id AS CHAR) LIKE CONCAT('%', filter_survey_search, '%'))
          AND (filter_survey_id IS NULL OR survey_detail.survey_id = filter_survey_id)
          AND (filter_sample_id IS NULL OR survey_master.sample_id = filter_sample_id)
          AND (filter_survey_date IS NULL OR DATE(survey_detail.survey_date) = DATE(filter_survey_date))
          AND (filter_start_date IS NULL OR DATE(survey_detail.start_date) >= DATE(filter_start_date))
          AND (filter_stop_date IS NULL OR DATE(survey_detail.stop_date) <= DATE(filter_stop_date))
          AND (filter_mail_flag IS NULL OR survey_master.mail_flag = filter_mail_flag)
          AND (filter_cawi_flag IS NULL OR survey_master.cawi_flag = filter_cawi_flag)
          AND (filter_cati_flag IS NULL OR survey_master.cati_flag = filter_cati_flag)
          AND (filter_capi_flag IS NULL OR survey_master.capi_flag = filter_capi_flag)
          AND (filter_hq_review_flag IS NULL OR survey_detail.hq_review_flag = filter_hq_review_flag)
    )
    SELECT
        filtered_survey_instances.*,
        COUNT(*) OVER() AS total_row_count
    FROM filtered_survey_instances
    ORDER BY
        filtered_survey_instances.survey_date DESC,
        filtered_survey_instances.survey_id,
        filtered_survey_instances.sample_id
    LIMIT row_limit OFFSET row_offset;
END$$

DELIMITER ;
