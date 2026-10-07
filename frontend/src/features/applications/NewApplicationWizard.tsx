import {
  Alert, Box, Button, FormControl, FormControlLabel, FormLabel, List, ListItem, ListItemText, Paper, Radio, RadioGroup,
  Stack, Step, StepLabel, Stepper, Typography,
} from '@mui/material';
import { useEffect, useRef, useState } from 'react';
import { Link as RouterLink, useNavigate } from 'react-router';
import { userMessage } from '@/api/problem';
import { LICENSE_TYPES, type LicenseType } from '@/api/types';
import { useNotify } from '@/app/Notifications';
import { DefinitionList } from '@/components/DefinitionList';
import { ErrorState } from '@/components/ErrorState';
import { LoadingState } from '@/components/LoadingState';
import { PageHeader } from '@/components/PageHeader';
import { useCurrentUser } from '@/features/auth/AuthContext';
import { useTrainingRecords } from '@/features/trainings/api';
import { t } from '@/i18n';
import { formatDate } from '@/utils/format';
import { useCreateApplication, useSubmitApplication } from './api';

const steps = [t.applications.steps.info, t.applications.steps.training, t.applications.steps.summary];

/**
 * Çok adımlı başvuru. Backend başvuruya sadece lisans türünü alır; kanıt eğitimi gönderim anında kendisi
 * seçer (en güncel başarılı kayıt). Bu yüzden "eğitim" adımı bir seçim değil, ön kontrol: uygun eğitim
 * yoksa kullanıcı ilerleyemez ve 422 almadan önce nedenini görür.
 */
export function NewApplicationWizard() {
  const user = useCurrentUser();
  const navigate = useNavigate();
  const notify = useNotify();
  const [activeStep, setActiveStep] = useState(0);
  const [licenseType, setLicenseType] = useState<LicenseType | null>(null);
  const [error, setError] = useState<string | null>(null);
  const headingRef = useRef<HTMLHeadingElement>(null);

  const trainings = useTrainingRecords({ PassedOnly: true, PageSize: 100 });
  const eligible = (trainings.data?.items ?? [])
    .filter((r) => r.licenseType === licenseType)
    .sort((a, b) => b.completedAtUtc.localeCompare(a.completedAtUtc));

  const create = useCreateApplication();
  const submit = useSubmitApplication();
  const pending = create.isPending || submit.isPending;

  // Adım değişince odak yeni adımın başlığına: ekran okuyucu kullanıcısı nerede olduğunu duyar.
  useEffect(() => headingRef.current?.focus(), [activeStep]);

  const save = async (andSubmit: boolean) => {
    if (!licenseType) return;
    setError(null);
    let draftId: string | undefined;
    try {
      const draft = await create.mutateAsync({ licenseType });
      draftId = draft.id;
      if (andSubmit) await submit.mutateAsync(draft.id);
      notify(andSubmit ? t.applications.submitted : t.applications.draftSaved);
      navigate(`/applications/${draft.id}`);
    } catch (e) {
      // Taslak oluştu ama gönderim başarısız olduysa kullanıcı taslağa yönlendirilir, oradan tekrar gönderebilir.
      if (draftId) {
        notify(userMessage(e), 'error');
        navigate(`/applications/${draftId}`);
      } else {
        setError(userMessage(e));
      }
    }
  };

  return (
    <>
      <PageHeader title={t.applications.newTitle} />
      <Stepper activeStep={activeStep} alternativeLabel sx={{ mb: 3 }}>
        {steps.map((label) => <Step key={label}><StepLabel>{label}</StepLabel></Step>)}
      </Stepper>

      <Paper sx={{ p: { xs: 2, md: 3 } }}>
        <Typography variant="h2" tabIndex={-1} ref={headingRef} sx={{ mb: 2, outline: 'none' }}>{steps[activeStep]}</Typography>
        {error && <Alert severity="error" sx={{ mb: 2 }} role="alert">{error}</Alert>}

        {activeStep === 0 && (
          <Stack spacing={3}>
            <DefinitionList items={[[t.applications.applicantInfo, user.fullName]]} />
            <FormControl>
              <FormLabel id="license-type-label">{t.applications.chooseType}</FormLabel>
              <RadioGroup
                aria-labelledby="license-type-label"
                value={licenseType ?? ''}
                onChange={(e) => setLicenseType(e.target.value as LicenseType)}
              >
                {LICENSE_TYPES.map((type) => (
                  <FormControlLabel key={type} value={type} control={<Radio />} label={t.licenseTypes[type]} />
                ))}
              </RadioGroup>
            </FormControl>
          </Stack>
        )}

        {activeStep === 1 && licenseType && (
          <>
            {trainings.isLoading && <LoadingState />}
            {!!trainings.error && <ErrorState error={trainings.error} onRetry={() => void trainings.refetch()} />}
            {trainings.data && (eligible.length === 0 ? (
              <Alert severity="warning">{t.applications.noEligibleTraining(licenseType)}</Alert>
            ) : (
              <>
                <Typography sx={{ mb: 1 }}>{t.applications.eligibleTrainings}</Typography>
                <List dense>
                  {eligible.map((r) => (
                    <ListItem key={r.id}>
                      <ListItemText primary={`${r.trainingOrgName} · ${t.trainings.score}: ${r.examScore}`} secondary={formatDate(r.completedAtUtc)} />
                    </ListItem>
                  ))}
                </List>
                <Alert severity="info">{t.applications.trainingNote}</Alert>
              </>
            ))}
          </>
        )}

        {activeStep === 2 && licenseType && (
          <DefinitionList
            items={[
              [t.applications.applicant, user.fullName],
              [t.applications.licenseType, t.licenseTypes[licenseType]],
              [t.applications.evidence, eligible[0] ? `${eligible[0].trainingOrgName} · ${formatDate(eligible[0].completedAtUtc)}` : '—'],
            ]}
          />
        )}

        <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1, mt: 3, justifyContent: 'space-between' }}>
          <Button
            onClick={() => (activeStep === 0 ? navigate('/applications') : setActiveStep((s) => s - 1))}
            disabled={pending}
          >
            {activeStep === 0 ? t.common.cancel : t.common.back}
          </Button>
          {activeStep < 2 ? (
            <Button
              variant="contained"
              onClick={() => setActiveStep((s) => s + 1)}
              disabled={!licenseType || (activeStep === 1 && eligible.length === 0)}
            >
              {t.common.next}
            </Button>
          ) : (
            <Stack direction="row" spacing={1}>
              <Button variant="outlined" onClick={() => void save(false)} disabled={pending}>{t.applications.saveDraft}</Button>
              <Button variant="contained" onClick={() => void save(true)} disabled={pending}>{t.applications.saveAndSubmit}</Button>
            </Stack>
          )}
        </Box>
      </Paper>
      <Button component={RouterLink} to="/trainings" sx={{ mt: 2 }}>{t.nav.myTrainings}</Button>
    </>
  );
}
